using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.Product.Request;

public class ProductDocumentDto
{
    [Required(ErrorMessage = "Document type is required")]
    [StringLength(50)]
    public string DocumentType { get; set; } = null!;

    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "URL is required")]
    [StringLength(500, ErrorMessage = "URL cannot exceed 500 characters")]
    public string Url { get; set; } = null!;

    public int DisplayOrder { get; set; } = 0;
}
