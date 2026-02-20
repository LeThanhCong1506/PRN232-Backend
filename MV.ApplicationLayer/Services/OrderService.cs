using Microsoft.Extensions.Configuration;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Order.Request;
using MV.DomainLayer.DTOs.Order.Response;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Enums;
using MV.DomainLayer.Interfaces;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepo;
    private readonly ICartRepository _cartRepo;
    private readonly StemDbContext _context;
    private readonly IConfiguration _configuration;

    public OrderService(
        IOrderRepository orderRepo,
        ICartRepository cartRepo,
        StemDbContext context,
        IConfiguration configuration)
    {
        _orderRepo = orderRepo;
        _cartRepo = cartRepo;
        _context = context;
        _configuration = configuration;
    }

    // ==================== CHECKOUT ====================

    public async Task<ApiResponse<CheckoutResponse>> CheckoutAsync(int userId, CreateOrderRequest request)
    {
        // 1. Validate payment method
        if (!Enum.TryParse<PaymentMethodEnum>(request.PaymentMethod, true, out var paymentMethod))
        {
            return ApiResponse<CheckoutResponse>.ErrorResponse("Invalid payment method. Only COD or SEPAY will be accepted.");
        }

        // 2. Get cart
        var cart = await _cartRepo.GetCartByUserIdAsync(userId);
        if (cart == null || !cart.CartItems.Any())
        {
            return ApiResponse<CheckoutResponse>.ErrorResponse("Shopping cart is empty.");
        }

        // 3. Validate stock
        foreach (var item in cart.CartItems)
        {
            var stock = item.Product.StockQuantity ?? 0;
            var qty = item.Quantity ?? 1;
            if (stock < qty)
            {
                return ApiResponse<CheckoutResponse>.ErrorResponse(
                    $"Product '{item.Product.Name}' only has {stock} left in stock, you requested {qty}.");
            }
        }

        // 4. Validate coupon
        Coupon? coupon = null;
        decimal discountAmount = 0;

        if (!string.IsNullOrEmpty(request.CouponCode))
        {
            coupon = await _orderRepo.GetCouponByCodeAsync(request.CouponCode);
            if (coupon == null)
            {
                return ApiResponse<CheckoutResponse>.ErrorResponse("The coupon code does not exist.");
            }

            var now = DateTime.Now;
            if (now < coupon.StartDate || now > coupon.EndDate)
            {
                return ApiResponse<CheckoutResponse>.ErrorResponse("The coupon code has expired or has not started.");
            }

            if (coupon.UsageLimit.HasValue && (coupon.UsedCount ?? 0) >= coupon.UsageLimit.Value)
            {
                return ApiResponse<CheckoutResponse>.ErrorResponse("The coupon code has expired.");
            }
        }

        // 5. Calculate amounts
        decimal subtotal = 0;
        foreach (var item in cart.CartItems)
        {
            subtotal += (item.Quantity ?? 1) * item.Product.Price;
        }

        decimal shippingFee = 5000; // Phí ship

        if (coupon != null)
        {
            if (coupon.MinOrderValue.HasValue && subtotal < coupon.MinOrderValue.Value)
            {
                return ApiResponse<CheckoutResponse>.ErrorResponse(
                    $"Minimum order value {coupon.MinOrderValue.Value:N0}đ to use the coupon");
            }

            var discountType = await _orderRepo.GetCouponDiscountTypeAsync(coupon.CouponId);
            if (discountType == DiscountTypeEnum.PERCENTAGE.ToString())
            {
                discountAmount = subtotal * coupon.DiscountValue / 100;
            }
            else
            {
                discountAmount = coupon.DiscountValue;
            }

            if (discountAmount > subtotal)
                discountAmount = subtotal;
        }

        decimal totalAmount = subtotal + shippingFee - discountAmount;

        // 6. Generate order number
        var todayCount = await _orderRepo.GetTodayOrderCountAsync();
        var orderNumber = $"ORD{DateTime.Now:yyyyMMdd}{(todayCount + 1):D3}";

        // 7. Build shipping address
        var shippingAddress = $"{request.StreetAddress}, {request.Ward}, {request.District}, {request.Province}";

        // 8. Transaction
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 8a. Create OrderHeader
            var order = new OrderHeader
            {
                UserId = userId,
                CouponId = coupon?.CouponId,
                OrderNumber = orderNumber,
                ShippingFee = shippingFee,
                SubtotalAmount = subtotal,
                DiscountAmount = discountAmount,
                TotalAmount = totalAmount,
                ShippingAddress = shippingAddress,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                CustomerPhone = request.CustomerPhone,
                Province = request.Province,
                District = request.District,
                Ward = request.Ward,
                StreetAddress = request.StreetAddress,
                Notes = request.Notes,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            await _orderRepo.CreateOrderAsync(order);

            // 8b. Set order status
            await _orderRepo.SetOrderStatusAsync(order.OrderId, OrderStatusEnum.PENDING.ToString());

            // 8c. Create OrderItems with product snapshot
            var orderItems = cart.CartItems.Select(ci => new OrderItem
            {
                OrderId = order.OrderId,
                ProductId = ci.ProductId,
                Quantity = ci.Quantity ?? 1,
                UnitPrice = ci.Product.Price,
                Subtotal = (ci.Quantity ?? 1) * ci.Product.Price,
                ProductName = ci.Product.Name,
                ProductSku = ci.Product.Sku,
                ProductImageUrl = ci.Product.ProductImages?.OrderBy(i => i.ImageId).FirstOrDefault()?.ImageUrl,
                CreatedAt = DateTime.Now
            }).ToList();
            await _orderRepo.CreateOrderItemsAsync(orderItems);

            // 8d. Decrement stock (check rows affected to prevent overselling)
            foreach (var ci in cart.CartItems)
            {
                var rowsAffected = await _orderRepo.DecrementStockAsync(ci.ProductId, ci.Quantity ?? 1);
                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException(
                        $"The product '{ci.Product.Name}' is out of stock.");
                }
            }

            // 8e. Increment coupon used count
            if (coupon != null)
            {
                await _orderRepo.IncrementCouponUsedCountAsync(coupon.CouponId);
            }

            // 8f. Create Payment
            var payment = new Payment
            {
                OrderId = order.OrderId,
                Amount = totalAmount,
                CreatedAt = DateTime.Now
            };

            string? checkoutUrl = null;
            if (paymentMethod == PaymentMethodEnum.SEPAY)
            {
                payment.PaymentReference = "STEM" + orderNumber.Substring(3);
                payment.ExpiredAt = DateTime.Now.AddMinutes(30);

                var sepayConfig = _configuration.GetSection("SePay");

                // Generate QR Code URL (hiển thị QR trực tiếp trên frontend)
                var accountNumber = sepayConfig["AccountNumber"];
                var bankName = sepayConfig["BankName"];
                var qrBaseUrl = sepayConfig["QrBaseUrl"] ?? "https://qr.sepay.vn/img";
                payment.QrCodeUrl = $"{qrBaseUrl}?bank={bankName}&acc={accountNumber}" +
                                    $"&template=compact&amount={totalAmount:F0}&des={payment.PaymentReference}";
            }

            // CreatePaymentAsync giờ INSERT kèm luôn payment_method và status (PostgreSQL enum)
            await _orderRepo.CreatePaymentAsync(payment, paymentMethod.ToString(), PaymentStatusEnum.PENDING.ToString());

            // 8h. Clear cart
            await _cartRepo.ClearCartAsync(cart.CartId);

            await transaction.CommitAsync();

            // 9. Generate checkout URL (trỏ về endpoint backend sẽ auto-POST form đến SePay)
            if (paymentMethod == PaymentMethodEnum.SEPAY)
            {
                var sepayConfig = _configuration.GetSection("SePay");
                var merchantId = sepayConfig["MerchantId"];
                var secretKey = sepayConfig["SecretKey"];

                if (!string.IsNullOrEmpty(merchantId) && merchantId != "YOUR_MERCHANT_ID_HERE"
                    && !string.IsNullOrEmpty(secretKey) && secretKey != "YOUR_SECRET_KEY_HERE")
                {
                    // URL đến endpoint GET /api/Payment/{orderId}/checkout
                    // Endpoint này sẽ render HTML form auto-submit POST đến SePay
                    checkoutUrl = $"/api/Payment/{order.OrderId}/checkout";
                }
            }

            // 10. Return response
            var response = new CheckoutResponse
            {
                OrderId = order.OrderId,
                OrderNumber = orderNumber,
                Status = OrderStatusEnum.PENDING.ToString(),
                TotalAmount = totalAmount,
                PaymentMethod = paymentMethod.ToString(),
                PaymentStatus = PaymentStatusEnum.PENDING.ToString(),
                PaymentReference = payment.PaymentReference,
                QrCodeUrl = payment.QrCodeUrl,
                CheckoutUrl = checkoutUrl,
                PaymentExpiredAt = payment.ExpiredAt,
                Message = paymentMethod == PaymentMethodEnum.COD
                    ? "Order successful. Payment upon delivery."
                    : "Order successful. Redirect to checkoutUrl to complete payment."
            };

            return ApiResponse<CheckoutResponse>.SuccessResponse(response, "Order placed successfully.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ApiResponse<CheckoutResponse>.ErrorResponse($"Order error: {ex.Message}");
        }
    }

    // ==================== GET MY ORDERS ====================

    public async Task<ApiResponse<PagedResponse<OrderResponse>>> GetMyOrdersAsync(
        int userId, OrderFilterRequest filter)
    {
        var (orders, totalCount) = await _orderRepo.GetOrdersByUserIdAsync(userId, filter);
        var response = await MapToOrderResponseList(orders, filter, totalCount);
        return ApiResponse<PagedResponse<OrderResponse>>.SuccessResponse(response);
    }

    // ==================== GET ALL ORDERS (ADMIN) ====================

    public async Task<ApiResponse<PagedResponse<OrderResponse>>> GetAllOrdersAsync(OrderFilterRequest filter)
    {
        var (orders, totalCount) = await _orderRepo.GetAllOrdersAsync(filter);
        var response = await MapToOrderResponseList(orders, filter, totalCount);
        return ApiResponse<PagedResponse<OrderResponse>>.SuccessResponse(response);
    }

    // ==================== GET ORDER DETAIL ====================

    public async Task<ApiResponse<OrderDetailResponse>> GetOrderDetailAsync(int orderId, int userId, bool isAdmin)
    {
        var order = await _orderRepo.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            return ApiResponse<OrderDetailResponse>.ErrorResponse("The order does not exist.");
        }

        if (!isAdmin && order.UserId != userId)
        {
            return ApiResponse<OrderDetailResponse>.ErrorResponse("Unauthorized: You do not have permission to view this order.");
        }

        var detail = await MapToOrderDetail(order);
        return ApiResponse<OrderDetailResponse>.SuccessResponse(detail);
    }

    // ==================== UPDATE ORDER STATUS ====================

    public async Task<ApiResponse<OrderDetailResponse>> UpdateOrderStatusAsync(
        int orderId, int adminUserId, UpdateOrderStatusRequest request)
    {
        var order = await _orderRepo.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            return ApiResponse<OrderDetailResponse>.ErrorResponse("The order does not exist.");
        }

        if (!Enum.TryParse<OrderStatusEnum>(request.Status, true, out var newStatus))
        {
            return ApiResponse<OrderDetailResponse>.ErrorResponse("Invalid status");
        }

        // Block CANCELLED via this endpoint — must use CancelOrder endpoint instead
        if (newStatus == OrderStatusEnum.CANCELLED)
        {
            return ApiResponse<OrderDetailResponse>.ErrorResponse(
                "This order cannot be canceled.");
        }

        var currentStatus = await _orderRepo.GetOrderStatusAsync(orderId);

        // Validate transition
        var validationError = ValidateStatusTransition(currentStatus, newStatus);
        if (validationError != null)
        {
            return ApiResponse<OrderDetailResponse>.ErrorResponse(validationError);
        }

        // Apply transition
        switch (newStatus)
        {
            case OrderStatusEnum.CONFIRMED:
                order.ConfirmedAt = DateTime.Now;
                order.ConfirmedBy = adminUserId;
                break;

            case OrderStatusEnum.SHIPPED:
                if (string.IsNullOrEmpty(request.TrackingNumber))
                {
                    return ApiResponse<OrderDetailResponse>.ErrorResponse("Tracking number is required when switching to SHIPPED.");
                }
                order.ShippedAt = DateTime.Now;
                order.ShippedBy = adminUserId;
                order.TrackingNumber = request.TrackingNumber;
                order.Carrier = request.Carrier;
                order.ExpectedDeliveryDate = request.ExpectedDeliveryDate;
                break;

            case OrderStatusEnum.DELIVERED:
                order.DeliveredAt = DateTime.Now;
                // If COD, mark payment as completed
                var paymentMethod = await _orderRepo.GetPaymentMethodByOrderIdAsync(orderId);
                if (paymentMethod == PaymentMethodEnum.COD.ToString())
                {
                    await _orderRepo.SetPaymentStatusByOrderIdAsync(orderId, PaymentStatusEnum.COMPLETED.ToString());
                    if (order.Payment != null)
                    {
                        order.Payment.PaymentDate = DateTime.Now;
                        order.Payment.ReceivedAmount = order.TotalAmount;
                    }
                }
                break;
        }

        order.UpdatedAt = DateTime.Now;
        await _orderRepo.UpdateOrderAsync(order);
        await _orderRepo.SetOrderStatusAsync(orderId, newStatus.ToString());

        // Reload and return
        order = await _orderRepo.GetOrderByIdAsync(orderId);
        var detail = await MapToOrderDetail(order!);
        return ApiResponse<OrderDetailResponse>.SuccessResponse(detail, "Status update successful");
    }

    // ==================== CANCEL ORDER ====================

    public async Task<ApiResponse<object>> CancelOrderAsync(
        int orderId, int userId, bool isAdmin, CancelOrderRequest request)
    {
        var order = await _orderRepo.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            return ApiResponse<object>.ErrorResponse("The order does not exist.");
        }

        if (!isAdmin && order.UserId != userId)
        {
            return ApiResponse<object>.ErrorResponse("Unauthorized: You do not have permission to view this order.");
        }

        var currentStatus = await _orderRepo.GetOrderStatusAsync(orderId);
        if (currentStatus != OrderStatusEnum.PENDING.ToString())
        {
            return ApiResponse<object>.ErrorResponse("Orders can only be canceled if they are in the PENDING status.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Set cancelled
            order.CancelledAt = DateTime.Now;
            order.CancelledBy = userId;
            order.CancelReason = request.CancelReason;
            order.UpdatedAt = DateTime.Now;
            await _orderRepo.UpdateOrderAsync(order);
            await _orderRepo.SetOrderStatusAsync(orderId, OrderStatusEnum.CANCELLED.ToString());

            // Set payment failed
            await _orderRepo.SetPaymentStatusByOrderIdAsync(orderId, PaymentStatusEnum.FAILED.ToString());

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
            return ApiResponse<object>.SuccessResponse(null!, "Order cancelled successfully.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ApiResponse<object>.ErrorResponse($"Error when canceling order: {ex.Message}");
        }
    }

    // ==================== PRIVATE HELPERS ====================

    private string? ValidateStatusTransition(string? currentStatus, OrderStatusEnum newStatus)
    {
        return (currentStatus, newStatus) switch
        {
            (null, OrderStatusEnum.PENDING) => null,
            ("PENDING", OrderStatusEnum.CONFIRMED) => null,
            ("CONFIRMED", OrderStatusEnum.SHIPPED) => null,
            ("SHIPPED", OrderStatusEnum.DELIVERED) => null,
            ("CANCELLED", _) => "The order has been cancelled and its status cannot be changed.",
            ("DELIVERED", _) => "The order has been successfully delivered and its status cannot be changed.",
            _ => $"Cannot switch from {currentStatus} to {newStatus}"
        };
    }

    private async Task<PagedResponse<OrderResponse>> MapToOrderResponseList(
        List<OrderHeader> orders, OrderFilterRequest filter, int totalCount)
    {
        var orderIds = orders.Select(o => o.OrderId).ToList();
        var statuses = await _orderRepo.GetOrderStatusesBatchAsync(orderIds);
        var paymentEnums = await _orderRepo.GetPaymentEnumsBatchAsync(orderIds);

        var dtos = orders.Select(o =>
        {
            statuses.TryGetValue(o.OrderId, out var status);
            paymentEnums.TryGetValue(o.OrderId, out var pEnums);

            return new OrderResponse
            {
                OrderId = o.OrderId,
                OrderNumber = o.OrderNumber,
                Status = status ?? "PENDING",
                CustomerName = o.CustomerName,
                CustomerPhone = o.CustomerPhone,
                SubtotalAmount = o.SubtotalAmount,
                ShippingFee = o.ShippingFee,
                DiscountAmount = o.DiscountAmount,
                TotalAmount = o.TotalAmount,
                PaymentMethod = pEnums.Method,
                PaymentStatus = pEnums.Status,
                ItemCount = o.OrderItems.Count,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            };
        }).ToList();

        // Filter by status in-memory if specified
        if (!string.IsNullOrEmpty(filter.Status))
        {
            dtos = dtos.Where(d => d.Status.Equals(filter.Status, StringComparison.OrdinalIgnoreCase)).ToList();
            totalCount = dtos.Count;
        }

        return new PagedResponse<OrderResponse>(dtos, filter.PageNumber, filter.PageSize, totalCount);
    }

    private async Task<OrderDetailResponse> MapToOrderDetail(OrderHeader order)
    {
        var status = await _orderRepo.GetOrderStatusAsync(order.OrderId) ?? "PENDING";

        OrderPaymentResponse? paymentResponse = null;
        if (order.Payment != null)
        {
            var pMethod = await _orderRepo.GetPaymentMethodByOrderIdAsync(order.OrderId) ?? "COD";
            var pStatus = await _orderRepo.GetPaymentStatusByOrderIdAsync(order.OrderId) ?? "PENDING";

            paymentResponse = new OrderPaymentResponse
            {
                PaymentId = order.Payment.PaymentId,
                PaymentMethod = pMethod,
                Status = pStatus,
                Amount = order.Payment.Amount,
                ReceivedAmount = order.Payment.ReceivedAmount,
                PaymentReference = order.Payment.PaymentReference,
                TransactionId = order.Payment.TransactionId,
                QrCodeUrl = order.Payment.QrCodeUrl,
                PaymentDate = order.Payment.PaymentDate,
                ExpiredAt = order.Payment.ExpiredAt
            };
        }

        OrderCouponResponse? couponResponse = null;
        if (order.Coupon != null)
        {
            couponResponse = new OrderCouponResponse
            {
                CouponId = order.Coupon.CouponId,
                Code = order.Coupon.Code,
                DiscountValue = order.Coupon.DiscountValue
            };
        }

        return new OrderDetailResponse
        {
            OrderId = order.OrderId,
            OrderNumber = order.OrderNumber,
            Status = status,
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            Province = order.Province,
            District = order.District,
            Ward = order.Ward,
            StreetAddress = order.StreetAddress,
            ShippingAddress = order.ShippingAddress,
            SubtotalAmount = order.SubtotalAmount,
            ShippingFee = order.ShippingFee,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            Notes = order.Notes,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            ConfirmedAt = order.ConfirmedAt,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt,
            CancelledAt = order.CancelledAt,
            CancelReason = order.CancelReason,
            TrackingNumber = order.TrackingNumber,
            Carrier = order.Carrier,
            ExpectedDeliveryDate = order.ExpectedDeliveryDate,
            Items = order.OrderItems.Select(oi => new OrderItemResponse
            {
                OrderItemId = oi.OrderItemId,
                ProductId = oi.ProductId,
                ProductName = oi.ProductName,
                ProductSku = oi.ProductSku,
                ProductImageUrl = oi.ProductImageUrl,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                DiscountAmount = oi.DiscountAmount,
                Subtotal = oi.Subtotal
            }).ToList(),
            Payment = paymentResponse,
            Coupon = couponResponse
        };
    }
}
