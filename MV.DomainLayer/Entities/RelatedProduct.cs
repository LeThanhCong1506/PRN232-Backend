using System;

namespace MV.DomainLayer.Entities;

public partial class RelatedProduct
{
    public int RelatedProductId { get; set; }

    public int ProductId { get; set; }

    public int RelatedToProductId { get; set; }

    public string RelationType { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Product RelatedToProduct { get; set; } = null!;
}
