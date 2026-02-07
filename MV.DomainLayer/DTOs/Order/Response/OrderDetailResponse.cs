namespace MV.DomainLayer.DTOs.Order.Response;

public class OrderDetailResponse
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = null!;
    public string Status { get; set; } = null!;

    // Customer snapshot
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Province { get; set; }
    public string? District { get; set; }
    public string? Ward { get; set; }
    public string? StreetAddress { get; set; }
    public string ShippingAddress { get; set; } = null!;

    // Amounts
    public decimal SubtotalAmount { get; set; }
    public decimal? ShippingFee { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    // Lifecycle
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    // Shipping
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public DateOnly? ExpectedDeliveryDate { get; set; }

    // Nested
    public List<OrderItemResponse> Items { get; set; } = new();
    public OrderPaymentResponse? Payment { get; set; }
    public OrderCouponResponse? Coupon { get; set; }
}

public class OrderItemResponse
{
    public int OrderItemId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductSku { get; set; }
    public string? ProductImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal Subtotal { get; set; }
}

public class OrderPaymentResponse
{
    public int PaymentId { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public string Status { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal? ReceivedAmount { get; set; }
    public string? PaymentReference { get; set; }
    public string? TransactionId { get; set; }
    public string? QrCodeUrl { get; set; }
    public DateTime? PaymentDate { get; set; }
    public DateTime? ExpiredAt { get; set; }
}

public class OrderCouponResponse
{
    public int CouponId { get; set; }
    public string Code { get; set; } = null!;
    public decimal DiscountValue { get; set; }
}
