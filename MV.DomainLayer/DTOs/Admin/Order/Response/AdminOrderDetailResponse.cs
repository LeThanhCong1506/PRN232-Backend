namespace MV.DomainLayer.DTOs.Admin.Order.Response;

public class AdminOrderDetailResponse
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string OrderNumber { get; set; } = null!;
    public string? Status { get; set; }

    // Customer info
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }

    // Address
    public string? ShippingAddress { get; set; }
    public string? Province { get; set; }
    public string? District { get; set; }
    public string? Ward { get; set; }
    public string? StreetAddress { get; set; }

    // Financial
    public decimal SubtotalAmount { get; set; }
    public decimal? ShippingFee { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Timestamps
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
    public string? Notes { get; set; }

    // Payment
    public AdminPaymentInfo? Payment { get; set; }

    // Items
    public List<AdminOrderItemResponse> Items { get; set; } = new();

    // Status transitions
    public List<string> AllowedStatusTransitions { get; set; } = new();

    public class AdminPaymentInfo
    {
        public int PaymentId { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }
        public decimal Amount { get; set; }
        public decimal? ReceivedAmount { get; set; }
        public string? PaymentReference { get; set; }
        public string? TransactionId { get; set; }
        public DateTime? PaymentDate { get; set; }
        public DateTime? ExpiredAt { get; set; }
        public string? Notes { get; set; }
    }

    public class AdminOrderItemResponse
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductSku { get; set; }
        public string? ProductImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
        public decimal? DiscountAmount { get; set; }
        public List<string> SerialNumbers { get; set; } = new();
        public bool HasSerialTracking => SerialNumbers.Count > 0;
    }
}
