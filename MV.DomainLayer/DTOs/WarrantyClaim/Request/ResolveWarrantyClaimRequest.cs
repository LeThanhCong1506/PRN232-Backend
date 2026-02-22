using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.WarrantyClaim.Request;

public class ResolveWarrantyClaimRequest
{
    [Required(ErrorMessage = "Resolution is required.")]
    [RegularExpression("^(APPROVED|REJECTED|RESOLVED)$", ErrorMessage = "Resolution must be APPROVED, REJECTED, or RESOLVED.")]
    public string Resolution { get; set; } = null!;

    public string? ResolutionNote { get; set; }
}
