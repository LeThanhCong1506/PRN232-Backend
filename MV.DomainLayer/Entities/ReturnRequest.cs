using System;

namespace MV.DomainLayer.Entities;

public partial class ReturnRequest
{
    public int ReturnRequestId { get; set; }

    public int OrderId { get; set; }

    public int UserId { get; set; }

    public string Type { get; set; } = null!;

    public string Reason { get; set; } = null!;

    public string Status { get; set; } = "SUBMITTED";

    public string? AdminNote { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? ProcessedBy { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public virtual OrderHeader Order { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual User? ProcessedByNavigation { get; set; }
}
