using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.Brand.Request;

public class UpdateBrandRequest
{
    [Required(ErrorMessage = "Brand name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Brand name must be between 2 and 100 characters.")]
    public string Name { get; set; } = null!;

    [StringLength(255, ErrorMessage = "Logo URL cannot exceed 255 characters.")]
    public string? LogoUrl { get; set; }
}
