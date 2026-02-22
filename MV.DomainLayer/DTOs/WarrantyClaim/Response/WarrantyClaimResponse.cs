namespace MV.DomainLayer.DTOs.WarrantyClaim.Response;

/// <summary>
/// Response cho Submit Warranty Claim (BE-4.3.2)
/// </summary>
public class SubmitWarrantyClaimResponse
{
    public int ClaimId { get; set; }
    public string Status { get; set; } = null!;
    public DateTime SubmittedAt { get; set; }
}

/// <summary>
/// Response cho Get All Warranty Claims - Admin (BE-4.3.3)
/// </summary>
public class AdminWarrantyClaimResponse
{
    public int ClaimId { get; set; }
    public string Status { get; set; } = null!;
    public ClaimCustomerInfo Customer { get; set; } = null!;
    public ClaimProductInfo Product { get; set; } = null!;
    public string IssueDescription { get; set; } = null!;
    public string? ContactPhone { get; set; }
    public string? ResolutionNote { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateOnly? ResolvedDate { get; set; }
}

public class ClaimCustomerInfo
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
}

public class ClaimProductInfo
{
    public int ProductId { get; set; }
    public string Name { get; set; } = null!;
}

/// <summary>
/// Response phân trang cho Admin Get All Claims (BE-4.3.3)
/// </summary>
public class AdminWarrantyClaimPagedResponse
{
    public List<AdminWarrantyClaimResponse> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Response cho Resolve Warranty Claim (BE-4.3.4)
/// </summary>
public class ResolveWarrantyClaimResponse
{
    public int ClaimId { get; set; }
    public string Status { get; set; } = null!;
    public string? ResolutionNote { get; set; }
    public DateOnly ResolvedDate { get; set; }
}
