using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Payment.Request;
using MV.DomainLayer.DTOs.Payment.Response;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Enums;
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

    public PaymentService(
        ISepayRepository sepayRepo,
        IOrderRepository orderRepo,
        StemDbContext context,
        IConfiguration configuration,
        ILogger<PaymentService> logger,
        INotificationService notificationService)
    {
        _sepayRepo = sepayRepo;
        _orderRepo = orderRepo;
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _notificationService = notificationService;
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
                CreatedAt = DateTime.Now
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
                sepayTransaction.ProcessedAt = DateTime.Now;
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
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update Payment record
                payment.PaymentDate = DateTime.Now;
                payment.ReceivedAmount = request.TransferAmount;
                payment.TransactionId = sepayId;
                payment.BankCode = request.Gateway;
                payment.GatewayResponse = JsonSerializer.Serialize(request);
                payment.UpdatedAt = DateTime.Now;
                await _orderRepo.UpdatePaymentAsync(payment);

                // Set payment status to COMPLETED
                await _orderRepo.SetPaymentStatusByOrderIdAsync(payment.OrderId, PaymentStatusEnum.COMPLETED.ToString());

                // Auto-confirm the order (payment received → order confirmed)
                var currentOrderStatus = await _orderRepo.GetOrderStatusAsync(payment.OrderId);
                if (currentOrderStatus == OrderStatusEnum.PENDING.ToString())
                {
                    await _orderRepo.SetOrderStatusAsync(payment.OrderId, OrderStatusEnum.CONFIRMED.ToString());
                    payment.Order.ConfirmedAt = DateTime.Now;
                    payment.Order.UpdatedAt = DateTime.Now;
                    await _orderRepo.UpdateOrderAsync(payment.Order);
                }

                // Mark SePay transaction as processed
                sepayTransaction.IsProcessed = true;
                sepayTransaction.ProcessedAt = DateTime.Now;
                sepayTransaction.OrderId = payment.OrderId;
                await _sepayRepo.UpdateTransactionAsync(sepayTransaction);

                await dbTransaction.CommitAsync();

                _logger.LogInformation(
                    "Payment completed successfully: OrderId={OrderId}, Amount={Amount}",
                    payment.OrderId, request.TransferAmount);

                // Notify realtime: payment confirmed
                try { await _notificationService.SendPaymentConfirmedAsync(payment.Order.UserId, payment.OrderId, payment.Order.OrderNumber, request.TransferAmount); } catch { }

                return ApiResponse<object>.SuccessResponse(null!, "Payment processed successfully");
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Error processing payment for OrderId={OrderId}", payment.OrderId);
                throw;
            }
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

        var isPaid = paymentStatus == PaymentStatusEnum.COMPLETED.ToString();
        var remainingSeconds = 0;
        if (payment.ExpiredAt.HasValue && !isPaid)
        {
            remainingSeconds = Math.Max(0, (int)(payment.ExpiredAt.Value - DateTime.Now).TotalSeconds);
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

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Update payment
            payment.PaymentDate = DateTime.Now;
            payment.ReceivedAmount = payment.Amount;
            payment.VerifiedBy = adminUserId;
            payment.VerifiedAt = DateTime.Now;
            payment.UpdatedAt = DateTime.Now;
            payment.Notes = "Xác nhận thủ công bởi Admin";
            await _orderRepo.UpdatePaymentAsync(payment);

            // Set payment status to COMPLETED
            await _orderRepo.SetPaymentStatusByOrderIdAsync(orderId, PaymentStatusEnum.COMPLETED.ToString());

            // Auto-confirm order if still PENDING
            var orderStatus = await _orderRepo.GetOrderStatusAsync(orderId);
            if (orderStatus == OrderStatusEnum.PENDING.ToString())
            {
                await _orderRepo.SetOrderStatusAsync(orderId, OrderStatusEnum.CONFIRMED.ToString());
                order.ConfirmedAt = DateTime.Now;
                order.ConfirmedBy = adminUserId;
                order.UpdatedAt = DateTime.Now;
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
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
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
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update payment record
                payment.PaymentDate = DateTime.Now;
                payment.ReceivedAmount = payment.Amount; // SePay đã xác nhận thanh toán thành công
                payment.UpdatedAt = DateTime.Now;
                payment.Notes = "Automatic confirmation from SePay success callback.";
                await _orderRepo.UpdatePaymentAsync(payment);

                // Set payment status to COMPLETED
                await _orderRepo.SetPaymentStatusByOrderIdAsync(order.OrderId, PaymentStatusEnum.COMPLETED.ToString());

                // Auto-confirm order if still PENDING
                var orderStatus = await _orderRepo.GetOrderStatusAsync(order.OrderId);
                if (orderStatus == OrderStatusEnum.PENDING.ToString())
                {
                    await _orderRepo.SetOrderStatusAsync(order.OrderId, OrderStatusEnum.CONFIRMED.ToString());
                    order.ConfirmedAt = DateTime.Now;
                    order.UpdatedAt = DateTime.Now;
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
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
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

    // ==================== EXPIRE OVERDUE PAYMENTS ====================

    public async Task ExpireOverduePaymentsAsync()
    {
        try
        {
            var expiredOrderIds = await _sepayRepo.GetExpiredPendingSepayOrderIdsAsync();

            if (!expiredOrderIds.Any())
                return;

            _logger.LogInformation("Found {Count} expired SEPAY payments to process", expiredOrderIds.Count);

            foreach (var orderId in expiredOrderIds)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var order = await _orderRepo.GetOrderByIdAsync(orderId);
                    if (order == null) continue;

                    // Set payment status to EXPIRED
                    await _orderRepo.SetPaymentStatusByOrderIdAsync(orderId, PaymentStatusEnum.EXPIRED.ToString());

                    if (order.Payment != null)
                    {
                        order.Payment.UpdatedAt = DateTime.Now;
                        await _orderRepo.UpdatePaymentAsync(order.Payment);
                    }

                    // Cancel the order
                    await _orderRepo.SetOrderStatusAsync(orderId, OrderStatusEnum.CANCELLED.ToString());
                    order.CancelledAt = DateTime.Now;
                    order.CancelReason = "Hết hạn thanh toán SEPAY (30 phút)";
                    order.UpdatedAt = DateTime.Now;
                    await _orderRepo.UpdateOrderAsync(order);

                    // Restore stock
                    foreach (var item in order.OrderItems)
                    {
                        await _orderRepo.IncrementStockAsync(item.ProductId, item.Quantity);
                    }

                    // Restore coupon
                    if (order.CouponId.HasValue)
                    {
                        await _orderRepo.DecrementCouponUsedCountAsync(order.CouponId.Value);
                    }

                    await transaction.CommitAsync();

                    // Notify realtime: payment expired
                    try { await _notificationService.SendPaymentExpiredAsync(order.UserId, orderId, order.OrderNumber); } catch { }

                    _logger.LogInformation("Expired and cancelled order: OrderId={OrderId}", orderId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error expiring payment for OrderId={OrderId}", orderId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExpireOverduePaymentsAsync");
        }
    }

    // ==================== PRIVATE HELPERS ====================

    /// <summary>
    /// Extract payment reference (e.g., "STEM20260207001") from transfer content.
    /// SePay content may have extra text, so we search for the STEM pattern.
    /// </summary>
    private static string? ExtractPaymentReference(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        // Normalize: uppercase, remove spaces
        var normalized = content.ToUpperInvariant().Replace(" ", "");

        // Look for STEM pattern: STEM + 8 digits (date) + 3 digits (sequence)
        var index = normalized.IndexOf("STEM", StringComparison.Ordinal);
        if (index < 0)
            return null;

        // STEM + 11 chars = "STEM20260207001"
        var remaining = normalized.Substring(index);
        if (remaining.Length < 15) // "STEM" (4) + "yyyyMMdd" (8) + "###" (3) = 15
            return null;

        var reference = remaining.Substring(0, 15);

        // Validate format: STEM + 11 digits
        var digitPart = reference.Substring(4);
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
