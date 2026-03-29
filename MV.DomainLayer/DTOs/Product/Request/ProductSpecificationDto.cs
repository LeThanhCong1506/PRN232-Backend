using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.Product.Request;

public class ProductSpecificationDto
{
    [Required(ErrorMessage = "Spec name is required")]
    [StringLength(100, ErrorMessage = "Spec name cannot exceed 100 characters")]
    public string SpecName { get; set; } = null!;

    [Required(ErrorMessage = "Spec value is required")]
    [StringLength(500, ErrorMessage = "Spec value cannot exceed 500 characters")]
    public string SpecValue { get; set; } = null!;

    public int DisplayOrder { get; set; } = 0;
}
