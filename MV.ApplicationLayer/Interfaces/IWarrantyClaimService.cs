using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.DTOs.WarrantyClaim.Request;
using MV.DomainLayer.DTOs.WarrantyClaim.Response;

namespace MV.ApplicationLayer.Interfaces;

public interface IWarrantyClaimService
{
    /// <summary>
    /// BE-4.3.2: Customer submit warranty claim
    /// </summary>
    Task<ApiResponse<SubmitWarrantyClaimResponse>> SubmitClaimAsync(int warrantyId, int userId, SubmitWarrantyClaimRequest request);

    /// <summary>
    /// BE-4.3.3: Admin get all warranty claims (paged, filter by status)
    /// </summary>
    Task<ApiResponse<AdminWarrantyClaimPagedResponse>> GetAllClaimsAsync(string? status, int page, int pageSize);

    /// <summary>
    /// BE-4.3.4: Admin resolve warranty claim
    /// </summary>
    Task<ApiResponse<ResolveWarrantyClaimResponse>> ResolveClaimAsync(int claimId, ResolveWarrantyClaimRequest request);

    /// <summary>
    /// Customer: Get my warranty claims (paged)
    /// </summary>
    Task<ApiResponse<AdminWarrantyClaimPagedResponse>> GetMyClaimsAsync(int userId, int page, int pageSize);

    /// <summary>
    /// Admin: Get single warranty claim by ID with full details
    /// </summary>
    Task<ApiResponse<AdminWarrantyClaimResponse>> GetClaimByIdAsync(int claimId);
}
