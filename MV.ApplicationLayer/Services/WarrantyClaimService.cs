using Microsoft.Extensions.Logging;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.DTOs.WarrantyClaim.Request;
using MV.DomainLayer.DTOs.WarrantyClaim.Response;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Helpers;
using MV.InfrastructureLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

public class WarrantyClaimService : IWarrantyClaimService
{
    private readonly IWarrantyClaimRepository _claimRepository;
    private readonly IWarrantyRepository _warrantyRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<WarrantyClaimService> _logger;

    public WarrantyClaimService(
        IWarrantyClaimRepository claimRepository,
        IWarrantyRepository warrantyRepository,
        INotificationService notificationService,
        ILogger<WarrantyClaimService> logger)
    {
        _claimRepository = claimRepository;
        _warrantyRepository = warrantyRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    // State machine: định nghĩa các transition hợp lệ
    private static readonly Dictionary<string, List<string>> AllowedTransitions = new()
    {
        { "SUBMITTED", new List<string> { "APPROVED", "REJECTED" } },
        { "APPROVED",  new List<string> { "RESOLVED", "UNRESOLVED" } },
        { "REJECTED",  new List<string>() }, // terminal
        { "RESOLVED",  new List<string>() }, // terminal
        { "UNRESOLVED", new List<string>() } // terminal
    };

    /// <summary>
    /// BE-4.3.2: Customer submit warranty claim
    /// Validation:
    /// - warrantyId phải thuộc về chính user đang đăng nhập
    /// - WARRANTY.isActive phải là true (không hết hạn, chưa bị VOID)
    /// - issueDescription: required, min 10, max 1000
    /// - Không được submit khi đã có claim đang SUBMITTED hoặc APPROVED (W3)
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
        var today = DateTimeHelper.VietnamTodayDateOnly();
        if (warranty.IsActive != true || warranty.EndDate < today)
        {
            return ApiResponse<SubmitWarrantyClaimResponse>.ErrorResponse("Warranty is not active or has expired.");
        }

        // 4. (W3) Chặn duplicate: không cho submit khi đã có claim đang xử lý
        var hasPending = warranty.WarrantyClaims?.Any(c =>
            c.Status == "SUBMITTED" || c.Status == "APPROVED") ?? false;
        if (hasPending)
        {
            return ApiResponse<SubmitWarrantyClaimResponse>.ErrorResponse(
                "This warranty already has a pending claim being processed.");
        }

        // 5. Tạo warranty claim
        var claim = new WarrantyClaim
        {
            WarrantyId = warrantyId,
            UserId = userId,
            ClaimDate = today,
            IssueDescription = request.IssueDescription,
            ContactPhone = request.ContactPhone,
            Status = "SUBMITTED",
            CreatedAt = DateTimeHelper.VietnamNow()
        };

        var created = await _claimRepository.CreateAsync(claim);

        // Notify Admin about new warranty claim
        try
        {
            var customerName = warranty.SerialNumberNavigation?.OrderItem?.Order?.CustomerName ?? "Unknown Customer";
            var productName = warranty.SerialNumberNavigation?.Product?.Name ?? "Unknown Product";
            await _notificationService.SendAdminNewWarrantyClaimAsync(created.ClaimId, customerName, productName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send admin notification for new warranty claim {ClaimId}", created.ClaimId);
        }

        var response = new SubmitWarrantyClaimResponse
        {
            ClaimId = created.ClaimId,
            Status = "SUBMITTED",
            SubmittedAt = created.CreatedAt ?? DateTimeHelper.VietnamNow()
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
    /// Business Logic (W1 State Machine):
    /// - SUBMITTED → APPROVED hoặc REJECTED
    /// - APPROVED  → RESOLVED hoặc REJECTED (lấy lại từ repair shop)
    /// - REJECTED / RESOLVED: terminal, không đổi được
    ///
    /// Side effects (W2):
    /// - APPROVED:  warranty.IsActive = false (đang sửa)
    /// - RESOLVED:  warranty.IsActive = true  (sửa xong)
    /// - UNRESOLVED: warranty.IsActive = true (không sửa được, trả lại)
    /// - REJECTED từ SUBMITTED: warranty không đổi
    /// </summary>
    public async Task<ApiResponse<ResolveWarrantyClaimResponse>> ResolveClaimAsync(int claimId, ResolveWarrantyClaimRequest request)
    {
        var claim = await _claimRepository.GetByIdAsync(claimId);
        if (claim == null)
        {
            return ApiResponse<ResolveWarrantyClaimResponse>.ErrorResponse($"Warranty claim with ID {claimId} not found.");
        }

        // (W1) Validate state machine transition
        var currentStatus = claim.Status ?? "SUBMITTED";
        var newStatus = request.Resolution.ToUpper();

        if (!AllowedTransitions.TryGetValue(currentStatus, out var allowedNext) || !allowedNext.Contains(newStatus))
        {
            return ApiResponse<ResolveWarrantyClaimResponse>.ErrorResponse(
                $"Cannot transition claim from '{currentStatus}' to '{newStatus}'. " +
                $"Allowed transitions: {string.Join(", ", allowedNext ?? new List<string>())}.");
        }

        // Cập nhật claim (W6: set cả Resolution field)
        claim.Status = newStatus;
        claim.Resolution = newStatus;
        claim.ResolutionNote = request.ResolutionNote;
        claim.ResolvedDate = DateTimeHelper.VietnamTodayDateOnly();

        await _claimRepository.UpdateAsync(claim);

        // (W2) Side effects trên warranty status
        if (claim.Warranty != null)
        {
            if (newStatus == "APPROVED")
            {
                // Warranty chuyển sang trạng thái IN_REPAIR → tạm vô hiệu
                claim.Warranty.IsActive = false;
                claim.Warranty.Notes = "IN_REPAIR - Claim approved, awaiting repair/replacement";
                await _warrantyRepository.UpdateAsync(claim.Warranty);
            }
            else if (newStatus == "RESOLVED")
            {
                // Warranty đã sửa xong → activate lại
                claim.Warranty.IsActive = true;
                claim.Warranty.Notes = "REPAIRED - Claim resolved successfully";
                await _warrantyRepository.UpdateAsync(claim.Warranty);
            }
            else if (newStatus == "UNRESOLVED")
            {
                // (W2) UNRESOLVED từ APPROVED: không sửa được, hoàn trả lại máy => activate lại warranty
                claim.Warranty.IsActive = true;
                claim.Warranty.Notes = "IRREPARABLE - Claim unresolved, device returned as is";
                await _warrantyRepository.UpdateAsync(claim.Warranty);
            }
            // REJECTED từ SUBMITTED: không thay đổi warranty
        }

        var response = new ResolveWarrantyClaimResponse
        {
            ClaimId = claim.ClaimId,
            Status = claim.Status!,
            ResolutionNote = claim.ResolutionNote,
            ResolvedDate = claim.ResolvedDate ?? DateTimeHelper.VietnamTodayDateOnly()
        };

        try
        {
            var productName = claim.Warranty?.SerialNumberNavigation?.Product?.Name ?? "your product";
            await _notificationService.SendWarrantyClaimStatusChangedAsync(
                claim.UserId, claim.ClaimId, productName, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send warranty claim notification for claim {ClaimId}", claim.ClaimId);
        }

        return ApiResponse<ResolveWarrantyClaimResponse>.SuccessResponse(response, $"Warranty claim {newStatus.ToLower()} successfully.");
    }

    /// <summary>
    /// Customer get my claims
    /// </summary>
    public async Task<ApiResponse<AdminWarrantyClaimPagedResponse>> GetMyClaimsAsync(int userId, int page, int pageSize)
    {
        var totalItems = await _claimRepository.CountByUserIdAsync(userId);
        var claims = await _claimRepository.GetByUserIdAsync(userId, page, pageSize);

        var items = claims.Select(c => new AdminWarrantyClaimResponse
        {
            ClaimId = c.ClaimId,
            Status = c.Status ?? "SUBMITTED",
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
}
