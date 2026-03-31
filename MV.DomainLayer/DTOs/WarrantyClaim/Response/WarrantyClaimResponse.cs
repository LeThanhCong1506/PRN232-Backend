namespace MV.DomainLayer.DTOs.WarrantyClaim.Response;

/// <summary>
/// Response cho Get My Warranty Claims - Customer (BE-4.3.1)
/// </summary>
public class CustomerWarrantyClaimResponse
{
    public int ClaimId { get; set; }
    public string Status { get; set; } = null!;
    public string SerialNumber { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string? ProductImageUrl { get; set; }
    public string PolicyName { get; set; } = null!;
    public DateOnly WarrantyExpiryDate { get; set; }
    public string IssueDescription { get; set; } = null!;
    public string? ResolutionNote { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateOnly? ResolvedDate { get; set; }
}

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
    public WarrantyClaimWarrantyInfo Warranty { get; set; } = null!;
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
    public string Sku { get; set; } = null!;
    public string? PrimaryImage { get; set; }
}

public class WarrantyClaimWarrantyInfo
{
    public int WarrantyId { get; set; }
    public string SerialNumber { get; set; } = null!;
    public string PolicyName { get; set; } = null!;
    public DateOnly ExpiryDate { get; set; }
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
