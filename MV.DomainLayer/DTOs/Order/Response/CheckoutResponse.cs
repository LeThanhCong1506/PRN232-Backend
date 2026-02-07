namespace MV.DomainLayer.DTOs.Order.Response;

public class CheckoutResponse
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = null!;
    public string Status { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public string PaymentStatus { get; set; } = null!;
    public string? PaymentReference { get; set; }
    public string? QrCodeUrl { get; set; }
    public DateTime? PaymentExpiredAt { get; set; }
    public string Message { get; set; } = null!;
}
