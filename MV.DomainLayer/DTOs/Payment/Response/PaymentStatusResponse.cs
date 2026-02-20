namespace MV.DomainLayer.DTOs.Payment.Response;

/// <summary>
/// Response cho frontend polling trạng thái thanh toán
/// </summary>
public class PaymentStatusResponse
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = null!;
    public string PaymentMethod { get; set; } = null!;
    public string PaymentStatus { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal? ReceivedAmount { get; set; }
    public string? QrCodeUrl { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public bool IsPaid { get; set; }
    public int RemainingSeconds { get; set; }
}
