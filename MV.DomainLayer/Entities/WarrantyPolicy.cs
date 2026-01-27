using System;
using System.Collections.Generic;

namespace MV.DomainLayer.Entities;

public partial class WarrantyPolicy
{
    public int PolicyId { get; set; }

    public string PolicyName { get; set; } = null!;

    public int DurationMonths { get; set; }

    public string? Description { get; set; }

    public string? TermsAndConditions { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<Warranty> Warranties { get; set; } = new List<Warranty>();
}
