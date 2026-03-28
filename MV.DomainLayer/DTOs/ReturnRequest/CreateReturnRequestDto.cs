using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.ReturnRequest;

public class CreateReturnRequestDto
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    [RegularExpression("^(RETURN|EXCHANGE)$", ErrorMessage = "Type must be RETURN or EXCHANGE")]
    public string Type { get; set; } = null!;

    [Required(ErrorMessage = "Reason is required")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Reason must be between 10 and 1000 characters")]
    public string Reason { get; set; } = null!;
}
