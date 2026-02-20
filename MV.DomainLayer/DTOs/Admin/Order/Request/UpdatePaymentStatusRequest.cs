using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.Admin.Order.Request;

public class UpdatePaymentStatusRequest
{
    [Required]
    public string NewPaymentStatus { get; set; } = null!;

    public string? Note { get; set; }
}
