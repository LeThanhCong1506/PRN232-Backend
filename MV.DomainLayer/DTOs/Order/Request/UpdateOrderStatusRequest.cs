using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.Order.Request;

public class UpdateOrderStatusRequest
{
    [Required(ErrorMessage = "Status is required")]
    public string Status { get; set; } = null!;

    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public DateOnly? ExpectedDeliveryDate { get; set; }
}
