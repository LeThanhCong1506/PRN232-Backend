namespace MV.DomainLayer.DTOs.Warranty.Response;

public class WarrantyResponse
{
    public int WarrantyId { get; set; }
    public string SerialNumber { get; set; } = null!;
    public int WarrantyPolicyId { get; set; }
    public string WarrantyPolicyName { get; set; } = null!;
    public int DurationMonths { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ActivationDate { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }

    // Product info
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductSku { get; set; }
}
