using System;
using System.Collections.Generic;

namespace MV.DomainLayer.Entities;

public partial class ProductBundle
{
    public int BundleId { get; set; }

    public int ParentProductId { get; set; }

    public int ChildProductId { get; set; }

    public int? Quantity { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Product ChildProduct { get; set; } = null!;

    public virtual Product ParentProduct { get; set; } = null!;
}
