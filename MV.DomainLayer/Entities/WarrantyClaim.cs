using System;
using System.Collections.Generic;

namespace MV.DomainLayer.Entities;

public partial class WarrantyClaim
{
    public int ClaimId { get; set; }

    public int WarrantyId { get; set; }

    public int UserId { get; set; }

    public DateOnly ClaimDate { get; set; }

    public string IssueDescription { get; set; } = null!;

    public string? Resolution { get; set; }

    public DateOnly? ResolvedDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Warranty Warranty { get; set; } = null!;
}
