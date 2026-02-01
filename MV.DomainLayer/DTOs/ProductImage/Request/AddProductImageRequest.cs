using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.ProductImage.Request;

public class AddProductImageRequest
{
    [Required(ErrorMessage = "Image URL is required")]
    [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters")]
    public string ImageUrl { get; set; } = null!;
}
