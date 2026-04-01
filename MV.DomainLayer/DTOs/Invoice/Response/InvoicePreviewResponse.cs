namespace MV.DomainLayer.DTOs.Invoice.Response;

public class InvoicePreviewResponse
{
    public string InvoiceNumber { get; set; } = null!;
    public string InvoiceType { get; set; } = null!;
    public DateTime IssuedAt { get; set; }

    public string TaxCode { get; set; } = null!;
    public string BillingName { get; set; } = null!;
    public string? RepresentativeName { get; set; }
    public string BillingAddress { get; set; } = null!;

    public OrderSnapshotInfo Order { get; set; } = new();
    public List<InvoiceItemInfo> Items { get; set; } = new();
    public PaymentSnapshotInfo? Payment { get; set; }

    public class OrderSnapshotInfo
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public decimal SubtotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class InvoiceItemInfo
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string? ProductSku { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class PaymentSnapshotInfo
    {
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? TransactionId { get; set; }
        public string? PaymentReference { get; set; }
        public decimal? ReceivedAmount { get; set; }
    }
}
