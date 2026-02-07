using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.Order.Request;

public class CancelOrderRequest
{
    [Required(ErrorMessage = "Cancel reason is required")]
    public string CancelReason { get; set; } = null!;
}
