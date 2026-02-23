using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.Admin.Order.Request;

public class AdminUpdateOrderStatusRequest
{
    [Required]
    public string NewStatus { get; set; } = null!;

    public string? Note { get; set; }

    public string? TrackingNumber { get; set; }

    public string? Carrier { get; set; }
}
