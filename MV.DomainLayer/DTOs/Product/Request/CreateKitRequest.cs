using System.ComponentModel.DataAnnotations;
using MV.DomainLayer.Enums;

namespace MV.DomainLayer.DTOs.Product.Request;

public class CreateKitRequest
{
    [Required(ErrorMessage = "Product name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 200 characters")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "SKU is required")]
    [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters")]
    public string Sku { get; set; } = null!;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Brand is required")]
    public int BrandId { get; set; }

    public int? WarrantyPolicyId { get; set; }

    public List<int> CategoryIds { get; set; } = new List<int>();

    [Required(ErrorMessage = "Components are required")]
    [MinLength(1, ErrorMessage = "KIT must have at least 1 component")]
    public List<KitComponentDto> Components { get; set; } = new List<KitComponentDto>();
}

public class KitComponentDto
{
    [Required(ErrorMessage = "Component product ID is required")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
}
