using System.ComponentModel.DataAnnotations;
using MV.DomainLayer.Enums;

namespace MV.DomainLayer.DTOs.Product.Request;

public class UpdateProductRequest
{
    [Required(ErrorMessage = "Product name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 200 characters")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "SKU is required")]
    [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters")]
    public string Sku { get; set; } = null!;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Product type is required")]
    public ProductTypeEnum ProductType { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
    public int StockQuantity { get; set; }

    [Required(ErrorMessage = "Brand is required")]
    public int BrandId { get; set; }

    public int? WarrantyPolicyId { get; set; }

    public bool HasSerialTracking { get; set; }

    public List<int> CategoryIds { get; set; } = new List<int>();
}
