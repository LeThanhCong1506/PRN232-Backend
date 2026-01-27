using System;
using System.Collections.Generic;

namespace MV.DomainLayer.Entities;

public partial class Warranty
{
    public int WarrantyId { get; set; }

    public string SerialNumber { get; set; } = null!;

    public int WarrantyPolicyId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? ActivationDate { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ProductInstance SerialNumberNavigation { get; set; } = null!;

    public virtual ICollection<WarrantyClaim> WarrantyClaims { get; set; } = new List<WarrantyClaim>();

    public virtual WarrantyPolicy WarrantyPolicy { get; set; } = null!;
}
