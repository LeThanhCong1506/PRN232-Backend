using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.DTOs.WarrantyClaim.Request;
using MV.DomainLayer.DTOs.WarrantyClaim.Response;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

public class WarrantyClaimService : IWarrantyClaimService
{
    private readonly IWarrantyClaimRepository _claimRepository;
    private readonly IWarrantyRepository _warrantyRepository;

    public WarrantyClaimService(IWarrantyClaimRepository claimRepository, IWarrantyRepository warrantyRepository)
    {
        _claimRepository = claimRepository;
        _warrantyRepository = warrantyRepository;
    }

    /// <summary>
    /// BE-4.3.2: Customer submit warranty claim
    /// Validation:
    /// - warrantyId phải thuộc về chính user đang đăng nhập
    /// - WARRANTY.isActive phải là true (không hết hạn, chưa bị VOID)
    /// - issueDescription: required, min 10, max 1000
    /// </summary>
    public async Task<ApiResponse<SubmitWarrantyClaimResponse>> SubmitClaimAsync(int warrantyId, int userId, SubmitWarrantyClaimRequest request)
    {
        // 1. Lấy warranty kèm thông tin order để xác minh ownership
        var warranty = await _warrantyRepository.GetByIdAsync(warrantyId);
        if (warranty == null)
        {
            return ApiResponse<SubmitWarrantyClaimResponse>.ErrorResponse("Warranty not found.");
        }

        // 2. Kiểm tra warranty thuộc về user đang đăng nhập
        var ownerUserId = warranty.SerialNumberNavigation?.OrderItem?.Order?.UserId;
        if (ownerUserId == null || ownerUserId != userId)
        {
            return ApiResponse<SubmitWarrantyClaimResponse>.ErrorResponse("This warranty does not belong to you.");
        }

        // 3. Kiểm tra warranty còn ACTIVE (isActive = true và chưa hết hạn)
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (warranty.IsActive != true || warranty.EndDate < today)
        {
            return ApiResponse<SubmitWarrantyClaimResponse>.ErrorResponse("Warranty is not active or has expired.");
        }

        // 4. Tạo warranty claim
        var claim = new WarrantyClaim
        {
            WarrantyId = warrantyId,
            UserId = userId,
            ClaimDate = today,
            IssueDescription = request.IssueDescription,
            ContactPhone = request.ContactPhone,
            Status = "SUBMITTED",
            CreatedAt = DateTime.Now
        };

        var created = await _claimRepository.CreateAsync(claim);

        var response = new SubmitWarrantyClaimResponse
        {
            ClaimId = created.ClaimId,
            Status = "SUBMITTED",
            SubmittedAt = created.CreatedAt ?? DateTime.Now
        };

        return ApiResponse<SubmitWarrantyClaimResponse>.SuccessResponse(response, "Warranty claim submitted successfully");
    }

    /// <summary>
    /// BE-4.3.3: Admin get all warranty claims (paged, filter by status)
    /// </summary>
    public async Task<ApiResponse<AdminWarrantyClaimPagedResponse>> GetAllClaimsAsync(string? status, int page, int pageSize)
    {
        var totalItems = await _claimRepository.CountAsync(status);
        var claims = await _claimRepository.GetAllAsync(status, page, pageSize);

        var items = claims.Select(c => new AdminWarrantyClaimResponse
        {
            ClaimId = c.ClaimId,
            Status = c.Status ?? "SUBMITTED",
            Customer = new ClaimCustomerInfo
            {
                UserId = c.UserId,
                FullName = c.User?.FullName ?? c.User?.Username ?? "Unknown",
                Phone = c.ContactPhone ?? c.User?.Phone
            },
            Product = new ClaimProductInfo
            {
                ProductId = c.Warranty?.SerialNumberNavigation?.ProductId ?? 0,
                Name = c.Warranty?.SerialNumberNavigation?.Product?.Name ?? "Unknown"
            },
            IssueDescription = c.IssueDescription,
            ContactPhone = c.ContactPhone,
            ResolutionNote = c.ResolutionNote,
            SubmittedAt = c.CreatedAt ?? DateTime.MinValue,
            ResolvedDate = c.ResolvedDate
        }).ToList();

        var response = new AdminWarrantyClaimPagedResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalItems > 0 ? (int)Math.Ceiling((double)totalItems / pageSize) : 0
        };

        return ApiResponse<AdminWarrantyClaimPagedResponse>.SuccessResponse(response);
    }

    /// <summary>
    /// BE-4.3.4: Admin resolve warranty claim
    /// Business Logic:
    /// - UPDATE WARRANTY_CLAIM.resolution = resolution, resolution_note = note
    /// - Nếu APPROVED: UPDATE WARRANTY.isActive = false (IN_REPAIR concept)
    /// - Nếu RESOLVED: WARRANTY vẫn giữ nguyên (REPAIRED concept)
    /// </summary>
    public async Task<ApiResponse<ResolveWarrantyClaimResponse>> ResolveClaimAsync(int claimId, ResolveWarrantyClaimRequest request)
    {
        var claim = await _claimRepository.GetByIdAsync(claimId);
        if (claim == null)
        {
            return ApiResponse<ResolveWarrantyClaimResponse>.ErrorResponse($"Warranty claim with ID {claimId} not found.");
        }

        // Cập nhật claim
        claim.Status = request.Resolution;
        claim.ResolutionNote = request.ResolutionNote;
        claim.ResolvedDate = DateOnly.FromDateTime(DateTime.Today);

        await _claimRepository.UpdateAsync(claim);

        // Side effects trên warranty status
        if (claim.Warranty != null)
        {
            if (request.Resolution == "APPROVED")
            {
                // Warranty chuyển sang trạng thái IN_REPAIR → tạm vô hiệu
                claim.Warranty.IsActive = false;
                claim.Warranty.Notes = "IN_REPAIR - Claim approved, awaiting repair/replacement";
                await _warrantyRepository.UpdateAsync(claim.Warranty);
            }
            else if (request.Resolution == "RESOLVED")
            {
                // Warranty đã sửa xong → activate lại
                claim.Warranty.IsActive = true;
                claim.Warranty.Notes = "REPAIRED - Claim resolved";
                await _warrantyRepository.UpdateAsync(claim.Warranty);
            }
            // REJECTED: không thay đổi warranty
        }

        var response = new ResolveWarrantyClaimResponse
        {
            ClaimId = claim.ClaimId,
            Status = claim.Status!,
            ResolutionNote = claim.ResolutionNote,
            ResolvedDate = claim.ResolvedDate ?? DateOnly.FromDateTime(DateTime.Today)
        };

        return ApiResponse<ResolveWarrantyClaimResponse>.SuccessResponse(response, $"Warranty claim {request.Resolution.ToLower()} successfully.");
    }
}
