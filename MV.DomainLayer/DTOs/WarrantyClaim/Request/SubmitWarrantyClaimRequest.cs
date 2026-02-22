using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.WarrantyClaim.Request;

public class SubmitWarrantyClaimRequest
{
    [Required(ErrorMessage = "Issue description is required.")]
    [MinLength(10, ErrorMessage = "Issue description must be at least 10 characters.")]
    [MaxLength(1000, ErrorMessage = "Issue description must not exceed 1000 characters.")]
    public string IssueDescription { get; set; } = null!;

    public string? ContactPhone { get; set; }
}
