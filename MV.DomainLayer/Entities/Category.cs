using System;
using System.Collections.Generic;

namespace MV.DomainLayer.Entities;

public partial class Category
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
