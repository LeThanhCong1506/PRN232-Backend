using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Payment.Request;
using MV.DomainLayer.DTOs.Payment.Response;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Enums;
using MV.DomainLayer.Helpers;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;
using System.Text.Json;

namespace MV.ApplicationLayer.Services;

public class PaymentService : IPaymentService
{
    private readonly ISepayRepository _sepayRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly StemDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;
    private readonly INotificationService _notificationService;
    private readonly IProductBundleRepository _bundleRepo;

    public PaymentService(
        ISepayRepository sepayRepo,
        IOrderRepository orderRepo,
        StemDbContext context,
        IConfiguration configuration,
        ILogger<PaymentService> logger,
        INotificationService notificationService,
        IProductBundleRepository bundleRepo)
    {
        _sepayRepo = sepayRepo;
        _orderRepo = orderRepo;
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _notificationService = notificationService;
        _bundleRepo = bundleRepo;
    }

    // ==================== PROCESS WEBHOOK ====================

    public async Task<ApiResponse<object>> ProcessWebhookAsync(SepayWebhookRequest request)
    {
        _logger.LogInformation("SePay webhook received: Id={Id}, Amount={Amount}, Content={Content}",
            request.Id, request.TransferAmount, request.Content);

        try
        {
            // 1. Validate: only process incoming transfers
            if (!string.Equals(request.TransferType, "in", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Skipping non-incoming transfer: Type={Type}", request.TransferType);
                return ApiResponse<object>.SuccessResponse(null!, "Ignored: not an incoming transfer");
            }

            // 2. Check duplicate (idempotent)
            var sepayId = request.Id.ToString();
            if (await _sepayRepo.TransactionExistsBySepayIdAsync(sepayId))
            {
                _logger.LogInformation("Duplicate webhook ignored: SepayId={SepayId}", sepayId);
                return ApiResponse<object>.SuccessResponse(null!, "Duplicate transaction ignored");
            }

            // 3. Log raw transaction first
            var sepayTransaction = new SepayTransaction
            {
                SepayId = sepayId,
                Gateway = request.Gateway,
                TransactionDate = ParseTransactionDate(request.TransactionDate),
                AccountNumber = request.AccountNumber,
                TransferType = request.TransferType,
                TransferAmount = request.TransferAmount,
                Accumulated = request.Accumulated,
                Code = request.Code,
                Content = request.Content,
                ReferenceNumber = request.ReferenceNumber,
                Description = request.Description,
                IsProcessed = false,
                RawData = JsonSerializer.Serialize(request),
                CreatedAt = DateTimeHelper.VietnamNow()
            };
            await _sepayRepo.CreateTransactionAsync(sepayTransaction);

            // 4. Extract payment reference from content
            var paymentReference = ExtractPaymentReference(request.Content);
            if (string.IsNullOrEmpty(paymentReference))
            {
                _logger.LogWarning("Could not extract payment reference from content: {Content}", request.Content);
                return ApiResponse<object>.SuccessResponse(null!, "No matching payment reference found");
            }

            // 5. Find matching payment
            var payment = await _sepayRepo.GetPaymentByReferenceAsync(paymentReference);
            if (payment == null)
            {
                _logger.LogWarning("No payment found for reference: {Reference}", paymentReference);
                return ApiResponse<object>.SuccessResponse(null!, "No matching payment found");
            }

            // 6. Check if payment is still PENDING
            var currentPaymentStatus = await _orderRepo.GetPaymentStatusByOrderIdAsync(payment.OrderId);
            if (currentPaymentStatus != PaymentStatusEnum.PENDING.ToString())
            {
                _logger.LogInformation("Payment already processed: OrderId={OrderId}, Status={Status}",
                    payment.OrderId, currentPaymentStatus);
                sepayTransaction.IsProcessed = true;
                sepayTransaction.ProcessedAt = DateTimeHelper.VietnamNow();
                sepayTransaction.OrderId = payment.OrderId;
                await _sepayRepo.UpdateTransactionAsync(sepayTransaction);
                return ApiResponse<object>.SuccessResponse(null!, "Payment already processed");
            }

            // 7. Verify amount (transfer amount >= expected amount)
            if (request.TransferAmount < payment.Amount)
            {
                _logger.LogWarning(
                    "Insufficient transfer amount: Expected={Expected}, Received={Received}, OrderId={OrderId}",
                    payment.Amount, request.TransferAmount, payment.OrderId);
                // Still log the transaction but don't complete the payment
                sepayTransaction.OrderId = payment.OrderId;
                await _sepayRepo.UpdateTransactionAsync(sepayTransaction);
                return ApiResponse<object>.SuccessResponse(null!, "Transfer amount insufficient");
            }

            // 8. Complete payment in a transaction
            // Dùng tracked entities + single SaveChangesAsync để đảm bảo atomic
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Update Payment record (payment đã tracked từ GetPaymentByReferenceAsync với AsTracking)
                    payment.PaymentDate = DateTimeHelper.VietnamNow();
                    payment.ReceivedAmount = request.TransferAmount;
                    payment.TransactionId = sepayId;
                    payment.BankCode = request.Gateway;
                    payment.GatewayResponse = JsonSerializer.Serialize(request);
                    payment.UpdatedAt = DateTimeHelper.VietnamNow();

                    // Set payment status to COMPLETED via raw SQL (enlist trong transaction tự động qua EF)
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE payment SET status = {0}::payment_status_enum WHERE order_id = {1}",
                        PaymentStatusEnum.COMPLETED.ToString(), payment.OrderId);

                    // Auto-confirm the order (payment received → order confirmed)
                    var currentOrderStatus = await _orderRepo.GetOrderStatusAsync(payment.OrderId);
                    if (currentOrderStatus == OrderStatusEnum.PENDING.ToString())
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            "UPDATE order_header SET status = {0}::order_status_enum WHERE order_id = {1}",
                            OrderStatusEnum.CONFIRMED.ToString(), payment.OrderId);

                        if (payment.Order != null)
                        {
                            payment.Order.ConfirmedAt = DateTimeHelper.VietnamNow();
                            payment.Order.UpdatedAt = DateTimeHelper.VietnamNow();
                        }
                    }

                    // Mark SePay transaction as processed
                    sepayTransaction.IsProcessed = true;
                    sepayTransaction.ProcessedAt = DateTimeHelper.VietnamNow();
                    sepayTransaction.OrderId = payment.OrderId;

                    // Single SaveChangesAsync cho tất cả tracked entity changes
                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    _logger.LogInformation(
                        "Payment completed successfully: OrderId={OrderId}, Amount={Amount}",
                        payment.OrderId, request.TransferAmount);

                    // Notify realtime: payment confirmed
                    if (payment.Order != null)
                    {
                        try { await _notificationService.SendPaymentConfirmedAsync(payment.Order.UserId, payment.OrderId, payment.Order.OrderNumber, request.TransferAmount); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogError(ex, "Error processing payment for OrderId={OrderId}", payment.OrderId);
                    throw;
                }
            });

            return ApiResponse<object>.SuccessResponse(null!, "Payment processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error processing SePay webhook: Id={Id}", request.Id);
            // Always return success to SePay to prevent retries for non-transient errors
            return ApiResponse<object>.SuccessResponse(null!, "Webhook received");
        }
    }

    // ==================== GET PAYMENT STATUS ====================

    public async Task<ApiResponse<PaymentStatusResponse>> GetPaymentStatusAsync(
        int orderId, int userId, bool isAdmin)
    {
        var order = await _orderRepo.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            return ApiResponse<PaymentStatusResponse>.ErrorResponse("The order does not exist.");
        }

        if (!isAdmin && order.UserId != userId)
        {
            return ApiResponse<PaymentStatusResponse>.ErrorResponse(
                "Unauthorized: You do not have permission to view this order.");
        }

        var payment = order.Payment;
        if (payment == null)
        {
            return ApiResponse<PaymentStatusResponse>.ErrorResponse("Payment information not found.");
        }

        var paymentMethod = await _orderRepo.GetPaymentMethodByOrderIdAsync(orderId) ?? "COD";
        var paymentStatus = await _orderRepo.GetPaymentStatusByOrderIdAsync(orderId) ?? "PENDING";

        // Lazy expiry: nếu PENDING + có ExpiredAt đã qua → expire inline (COD hoặc SEPAY)
        if (paymentStatus == PaymentStatusEnum.PENDING.ToString()
            && payment.ExpiredAt.HasValue
            && payment.ExpiredAt.Value < DateTimeHelper.VietnamNow())
        {
            if (await TryExpireSinglePaymentAsync(orderId))
                paymentStatus = PaymentStatusEnum.EXPIRED.ToString();
        }

        var isPaid = paymentStatus == PaymentStatusEnum.COMPLETED.ToString();
        var remainingSeconds = 0;
        if (payment.ExpiredAt.HasValue && !isPaid)
        {
            remainingSeconds = Math.Max(0, (int)(payment.ExpiredAt.Value - DateTimeHelper.VietnamNow()).TotalSeconds);
        }

        var response = new PaymentStatusResponse
        {
            OrderId = orderId,
            OrderNumber = order.OrderNumber,
            PaymentMethod = paymentMethod,
            PaymentStatus = paymentStatus,
            Amount = payment.Amount,
            ReceivedAmount = payment.ReceivedAmount,
            QrCodeUrl = payment.QrCodeUrl,
            PaymentReference = payment.PaymentReference,
            ExpiredAt = payment.ExpiredAt,
            IsPaid = isPaid,
            RemainingSeconds = remainingSeconds
        };

        return ApiResponse<PaymentStatusResponse>.SuccessResponse(response);
    }

    // ==================== MANUAL VERIFY (ADMIN) ====================

    public async Task<ApiResponse<PaymentStatusResponse>> VerifyPaymentManuallyAsync(
        int orderId, int adminUserId)
    {
        var order = await _orderRepo.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            return ApiResponse<PaymentStatusResponse>.ErrorResponse("The order does not exist.");
        }

        var payment = order.Payment;
        if (payment == null)
        {
            return ApiResponse<PaymentStatusResponse>.ErrorResponse("Payment information not found.");
        }

        // Check payment method must be SEPAY
        var paymentMethod = await _orderRepo.GetPaymentMethodByOrderIdAsync(orderId);
        if (paymentMethod != PaymentMethodEnum.SEPAY.ToString())
        {
            return ApiResponse<PaymentStatusResponse>.ErrorResponse(
                "Manual confirmation is only possible for orders paid via SEPAY.");
        }

        // Check payment status must be PENDING
        var currentStatus = await _orderRepo.GetPaymentStatusByOrderIdAsync(orderId);
        if (currentStatus != PaymentStatusEnum.PENDING.ToString())
        {
            return ApiResponse<PaymentStatusResponse>.ErrorResponse(
                $"Payment cannot be confirmed in this status {currentStatus}.");
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Update payment
                    payment.PaymentDate = DateTimeHelper.VietnamNow();
                    payment.ReceivedAmount = payment.Amount;
                    payment.VerifiedBy = adminUserId;
                    payment.VerifiedAt = DateTimeHelper.VietnamNow();
                    payment.UpdatedAt = DateTimeHelper.VietnamNow();
                    payment.Notes = "Xác nhận thủ công bởi Admin";
                    await _orderRepo.UpdatePaymentAsync(payment);

                    // Set payment status to COMPLETED
                    await _orderRepo.SetPaymentStatusByOrderIdAsync(orderId, PaymentStatusEnum.COMPLETED.ToString());

                    // Auto-confirm order if still PENDING
                    var orderStatus = await _orderRepo.GetOrderStatusAsync(orderId);
                    if (orderStatus == OrderStatusEnum.PENDING.ToString())
                    {
                        await _orderRepo.SetOrderStatusAsync(orderId, OrderStatusEnum.CONFIRMED.ToString());
                        order.ConfirmedAt = DateTimeHelper.VietnamNow();
                        order.ConfirmedBy = adminUserId;
                        order.UpdatedAt = DateTimeHelper.VietnamNow();
                        await _orderRepo.UpdateOrderAsync(order);
                    }

                    await transaction.CommitAsync();

                    // Return updated status
                    var response = new PaymentStatusResponse
                    {
                        OrderId = orderId,
                        OrderNumber = order.OrderNumber,
                        PaymentMethod = PaymentMethodEnum.SEPAY.ToString(),
                        PaymentStatus = PaymentStatusEnum.COMPLETED.ToString(),
                        Amount = payment.Amount,
                        ReceivedAmount = payment.ReceivedAmount,
                        QrCodeUrl = payment.QrCodeUrl,
                        PaymentReference = payment.PaymentReference,
                        ExpiredAt = payment.ExpiredAt,
                        IsPaid = true,
                        RemainingSeconds = 0
                    };

                    return ApiResponse<PaymentStatusResponse>.SuccessResponse(
                        response, "Payment confirmed.");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            return ApiResponse<PaymentStatusResponse>.ErrorResponse(
                $"Error confirming payment: {ex.Message}");
        }
    }

    // ==================== PROCESS SUCCESS CALLBACK ====================

    public async Task<ApiResponse<PaymentStatusResponse>> ProcessSuccessCallbackAsync(string orderInvoiceNumber)
    {
        _logger.LogInformation("SePay success callback received: OrderInvoiceNumber={OrderNumber}", orderInvoiceNumber);

        try
        {
            // 1. Tìm order theo OrderNumber (chính là order_invoice_number gửi lên SePay)
            var order = await _orderRepo.GetOrderByOrderNumberAsync(orderInvoiceNumber);
            if (order == null)
            {
                _logger.LogWarning("Success callback: Order not found for OrderNumber={OrderNumber}", orderInvoiceNumber);
                return ApiResponse<PaymentStatusResponse>.ErrorResponse("The order does not exist.");
            }

            var payment = order.Payment;
            if (payment == null)
            {
                return ApiResponse<PaymentStatusResponse>.ErrorResponse("Payment information not found.");
            }

            // 2. Kiểm tra payment method phải là SEPAY
            var paymentMethod = await _orderRepo.GetPaymentMethodByOrderIdAsync(order.OrderId);
            if (paymentMethod != PaymentMethodEnum.SEPAY.ToString())
            {
                return ApiResponse<PaymentStatusResponse>.ErrorResponse("Orders not using the SEPAY payment method.");
            }

            // 3. Kiểm tra nếu đã COMPLETED thì trả về luôn (idempotent)
            var currentStatus = await _orderRepo.GetPaymentStatusByOrderIdAsync(order.OrderId);
            if (currentStatus == PaymentStatusEnum.COMPLETED.ToString())
            {
                _logger.LogInformation("Success callback: Payment already completed for OrderId={OrderId}", order.OrderId);
                return ApiResponse<PaymentStatusResponse>.SuccessResponse(new PaymentStatusResponse
                {
                    OrderId = order.OrderId,
                    OrderNumber = order.OrderNumber,
                    PaymentMethod = paymentMethod,
                    PaymentStatus = PaymentStatusEnum.COMPLETED.ToString(),
                    Amount = payment.Amount,
                    ReceivedAmount = payment.ReceivedAmount,
                    IsPaid = true,
                    RemainingSeconds = 0
                }, "Thanh toán đã hoàn tất trước đó");
            }

            // 4. Chỉ xử lý nếu đang PENDING
            if (currentStatus != PaymentStatusEnum.PENDING.ToString())
            {
                return ApiResponse<PaymentStatusResponse>.ErrorResponse(
                    $"Payment cannot be confirmed in this status {currentStatus}.");
            }

            // 5. Cập nhật payment status → COMPLETED, order status → CONFIRMED
            var strategy = _context.Database.CreateExecutionStrategy();
            try
            {
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // Update payment record
                        payment.PaymentDate = DateTimeHelper.VietnamNow();
                        payment.ReceivedAmount = payment.Amount; // SePay đã xác nhận thanh toán thành công
                        payment.UpdatedAt = DateTimeHelper.VietnamNow();
                        payment.Notes = "Automatic confirmation from SePay success callback.";
                        await _orderRepo.UpdatePaymentAsync(payment);

                        // Set payment status to COMPLETED
                        await _orderRepo.SetPaymentStatusByOrderIdAsync(order.OrderId, PaymentStatusEnum.COMPLETED.ToString());

                        // Auto-confirm order if still PENDING
                        var orderStatus = await _orderRepo.GetOrderStatusAsync(order.OrderId);
                        if (orderStatus == OrderStatusEnum.PENDING.ToString())
                        {
                            await _orderRepo.SetOrderStatusAsync(order.OrderId, OrderStatusEnum.CONFIRMED.ToString());
                            order.ConfirmedAt = DateTimeHelper.VietnamNow();
                            order.UpdatedAt = DateTimeHelper.VietnamNow();
                            await _orderRepo.UpdateOrderAsync(order);
                        }

                        await transaction.CommitAsync();

                        _logger.LogInformation(
                            "Success callback: Payment completed for OrderId={OrderId}, OrderNumber={OrderNumber}",
                            order.OrderId, order.OrderNumber);

                        // Notify realtime: payment confirmed
                        try { await _notificationService.SendPaymentConfirmedAsync(order.UserId, order.OrderId, order.OrderNumber, payment.Amount); } catch { }

                        return ApiResponse<PaymentStatusResponse>.SuccessResponse(new PaymentStatusResponse
                        {
                            OrderId = order.OrderId,
                            OrderNumber = order.OrderNumber,
                            PaymentMethod = PaymentMethodEnum.SEPAY.ToString(),
                            PaymentStatus = PaymentStatusEnum.COMPLETED.ToString(),
                            Amount = payment.Amount,
                            ReceivedAmount = payment.ReceivedAmount,
                            QrCodeUrl = payment.QrCodeUrl,
                            PaymentReference = payment.PaymentReference,
                            ExpiredAt = payment.ExpiredAt,
                            IsPaid = true,
                            RemainingSeconds = 0
                        }, "Payment successful.");
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing success callback for OrderId={OrderId}", order.OrderId);
                return ApiResponse<PaymentStatusResponse>.ErrorResponse(
                    $"Lỗi khi xử lý callback: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in ProcessSuccessCallbackAsync: OrderNumber={OrderNumber}", orderInvoiceNumber);
            return ApiResponse<PaymentStatusResponse>.ErrorResponse("System error when processing callbacks.");
        }
    }

    // ==================== LAZY EXPIRY ====================

    public async Task<string> CheckAndGetPaymentStatusAsync(int orderId)
    {
        // Single query: lấy status + method + expired_at cùng lúc thay vì 4 queries riêng lẻ
        var paymentInfo = await _context.Payments
            .Where(p => p.OrderId == orderId)
            .Select(p => new { p.ExpiredAt })
            .FirstOrDefaultAsync();

        var paymentStatus = await _orderRepo.GetPaymentStatusByOrderIdAsync(orderId);
        if (paymentStatus == null || paymentInfo == null) return paymentStatus ?? "UNKNOWN";

        // Chỉ check expiry nếu PENDING + đã hết hạn
        if (paymentStatus == PaymentStatusEnum.PENDING.ToString()
            && paymentInfo.ExpiredAt.HasValue
            && paymentInfo.ExpiredAt.Value < DateTimeHelper.VietnamNow())
        {
            // Verify payment method is SEPAY trước khi expire
            var paymentMethod = await _orderRepo.GetPaymentMethodByOrderIdAsync(orderId);
            if (paymentMethod == PaymentMethodEnum.SEPAY.ToString())
            {
                if (await TryExpireSinglePaymentAsync(orderId))
                    return PaymentStatusEnum.EXPIRED.ToString();
            }
        }

        return paymentStatus;
    }

    /// <summary>
    /// Expire single payment: set EXPIRED, cancel order, restore stock + coupon.
    /// Returns true nếu đã expire thành công.
    /// </summary>
    private async Task<bool> TryExpireSinglePaymentAsync(int orderId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var order = await _orderRepo.GetOrderByIdAsync(orderId);
                    if (order == null) return;

                    // Double-check: payment vẫn PENDING không (tránh race condition)
                    var currentPaymentStatus = await _orderRepo.GetPaymentStatusByOrderIdAsync(orderId);
                    if (currentPaymentStatus != PaymentStatusEnum.PENDING.ToString()) return;

                    // Guard: chỉ cancel đơn ở trạng thái PENDING
                    // Đơn COD đã được admin CONFIRM thì KHÔNG cancel dù ExpiredAt đã qua
                    var currentOrderStatus = await _orderRepo.GetOrderStatusAsync(orderId);
                    if (currentOrderStatus != OrderStatusEnum.PENDING.ToString()) return;

                    // Set payment status to EXPIRED
                    await _orderRepo.SetPaymentStatusByOrderIdAsync(orderId, PaymentStatusEnum.EXPIRED.ToString());

                    if (order.Payment != null)
                    {
                        order.Payment.UpdatedAt = DateTimeHelper.VietnamNow();
                        await _orderRepo.UpdatePaymentAsync(order.Payment);
                    }

                    // Cancel the order
                    var paymentMethod = await _orderRepo.GetPaymentMethodByOrderIdAsync(orderId);
                    await _orderRepo.SetOrderStatusAsync(orderId, OrderStatusEnum.CANCELLED.ToString());
                    order.CancelledAt = DateTimeHelper.VietnamNow();
                    order.CancelReason = paymentMethod == PaymentMethodEnum.COD.ToString()
                        ? "Đơn hàng COD tự động hủy: không được xác nhận trong 48 giờ"
                        : "Hết hạn thanh toán SEPAY";
                    order.UpdatedAt = DateTimeHelper.VietnamNow();
                    await _orderRepo.UpdateOrderAsync(order);

                    // Restore stock (KIT restore từng component, sản phẩm thường restore trực tiếp)
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Product?.ProductType == ProductTypeEnum.KIT.ToString())
                        {
                            var components = await _bundleRepo.GetBundleComponentsAsync(item.ProductId);
                            foreach (var comp in components)
                            {
                                var restoreQty = (comp.Quantity ?? 1) * item.Quantity;
                                await _orderRepo.IncrementStockAsync(comp.ChildProductId, restoreQty);
                            }
                        }
                        else
                        {
                            await _orderRepo.IncrementStockAsync(item.ProductId, item.Quantity);
                        }
                    }

                    // Restore coupon
                    if (order.CouponId.HasValue)
                    {
                        await _orderRepo.DecrementCouponUsedCountAsync(order.CouponId.Value);
                    }

                    await transaction.CommitAsync();

                    // Notify realtime: payment expired
                    try { await _notificationService.SendPaymentExpiredAsync(order.UserId, orderId, order.OrderNumber); } catch { }

                    _logger.LogInformation("Lazy expiry: expired and cancelled OrderId={OrderId}", orderId);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in lazy expiry for OrderId={OrderId}", orderId);
            return false;
        }
    }

    // ==================== BACKGROUND JOB: EXPIRE OVERDUE PAYMENTS ====================

    public async Task ExpireOverduePaymentsAsync()
    {
        // Lấy tất cả orderId có payment PENDING + SEPAY + đã quá hạn
        var overdueOrderIds = await _context.Payments
            .Where(p => p.ExpiredAt.HasValue && p.ExpiredAt.Value < DateTimeHelper.VietnamNow())
            .Select(p => p.OrderId)
            .ToListAsync();

        if (!overdueOrderIds.Any()) return;

        // Lọc những cái còn PENDING payment status (dùng raw SQL vì status là enum)
        // Xử lý cả COD (timeout 48h) và SEPAY (timeout 10 phút)
        var pendingOverdue = new List<int>();
        foreach (var orderId in overdueOrderIds)
        {
            var status = await _orderRepo.GetPaymentStatusByOrderIdAsync(orderId);
            var method = await _orderRepo.GetPaymentMethodByOrderIdAsync(orderId);
            if (status == PaymentStatusEnum.PENDING.ToString()
                && (method == PaymentMethodEnum.SEPAY.ToString()
                    || method == PaymentMethodEnum.COD.ToString()))
            {
                pendingOverdue.Add(orderId);
            }
        }

        _logger.LogInformation("PaymentExpiryJob: Found {Count} overdue payments to expire (COD + SEPAY).", pendingOverdue.Count);

        foreach (var orderId in pendingOverdue)
        {
            await TryExpireSinglePaymentAsync(orderId);
        }
    }

    // ==================== PRIVATE HELPERS ====================

    /// <summary>
    /// Extract payment reference (e.g., "SEVQR20260207001") from transfer content.
    /// SePay content may have extra text, so we search for the SEVQR pattern.
    /// </summary>
    private static string? ExtractPaymentReference(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        // Normalize: uppercase, remove spaces
        var normalized = content.ToUpperInvariant().Replace(" ", "");

        // Look for SEVQR pattern: SEVQR + 8 digits (date) + 3 digits (sequence)
        var index = normalized.IndexOf("SEVQR", StringComparison.Ordinal);
        if (index < 0)
            return null;

        // SEVQR + 11 chars = "SEVQR20260207001"
        var remaining = normalized.Substring(index);
        if (remaining.Length < 16) // "SEVQR" (5) + "yyyyMMdd" (8) + "###" (3) = 16
            return null;

        var reference = remaining.Substring(0, 16);

        // Validate format: SEVQR + 11 digits
        var digitPart = reference.Substring(5);
        if (digitPart.All(char.IsDigit))
            return reference;

        return null;
    }

    /// <summary>
    /// Parse transaction date from SePay format (yyyy-MM-dd HH:mm:ss)
    /// </summary>
    private static DateTime? ParseTransactionDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr))
            return null;

        if (DateTime.TryParse(dateStr, out var result))
            return result;

        return null;
    }
}
