using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.Warranty.Request;

public class CreateWarrantyRequest
{
    [Required(ErrorMessage = "Serial number is required")]
    [StringLength(100, ErrorMessage = "Serial number cannot exceed 100 characters")]
    public string SerialNumber { get; set; } = null!;

    [Required(ErrorMessage = "Warranty policy is required")]
    public int WarrantyPolicyId { get; set; }

    [Required(ErrorMessage = "Start date is required")]
    public DateOnly StartDate { get; set; }

    [Required(ErrorMessage = "End date is required")]
    public DateOnly EndDate { get; set; }

    public string? Notes { get; set; }
}
