using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.ReturnRequest;

public class ProcessReturnRequestDto
{
    [Required]
    [RegularExpression("^(APPROVED|REJECTED|COMPLETED)$", ErrorMessage = "Status must be APPROVED, REJECTED, or COMPLETED")]
    public string Status { get; set; } = null!;

    [StringLength(1000)]
    public string? AdminNote { get; set; }
}
