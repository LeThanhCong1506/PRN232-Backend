namespace MV.DomainLayer.DTOs.Admin.Order.Response;

public class AdminOrderResponse
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = null!;
    public string? Status { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }
    public int ItemCount { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
