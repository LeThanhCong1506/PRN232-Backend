using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.Product.Request;

public class CreateRelatedProductDto
{
    [Required]
    public int RelatedToProductId { get; set; }

    [Required]
    [StringLength(30)]
    public string RelationType { get; set; } = "SIMILAR";

    public int DisplayOrder { get; set; } = 0;
}
