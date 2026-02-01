using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.ProductBundle.Request;

public class AddProductToBundleRequest
{
    [Required(ErrorMessage = "Child product ID is required")]
    public int ChildProductId { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
}
