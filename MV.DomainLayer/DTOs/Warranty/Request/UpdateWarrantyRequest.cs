using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.Warranty.Request;

public class UpdateWarrantyRequest
{
    [Required(ErrorMessage = "End date is required")]
    public DateOnly EndDate { get; set; }

    public bool IsActive { get; set; }

    public string? Notes { get; set; }
}
