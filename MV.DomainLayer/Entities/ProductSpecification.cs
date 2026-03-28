using System;

namespace MV.DomainLayer.Entities;

public partial class ProductSpecification
{
    public int SpecificationId { get; set; }

    public int ProductId { get; set; }

    public string SpecName { get; set; } = null!;

    public string SpecValue { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public virtual Product Product { get; set; } = null!;
}
