using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Admin.Coupon.Request;
using MV.DomainLayer.DTOs.Admin.Coupon.Response;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

public class CouponService : ICouponService
{
    private readonly ICouponRepository _couponRepository;

    public CouponService(ICouponRepository couponRepository)
    {
        _couponRepository = couponRepository;
    }

    public async Task<ApiResponse<List<AdminCouponResponse>>> GetAllCouponsAsync()
    {
        try
        {
            var rows = await _couponRepository.GetAllCouponsAsync();
            var now = DateTime.Now;
            var list = rows.Select(r => MapToResponse(r.Coupon, r.DiscountType, r.OrderCount, now)).ToList();
            return ApiResponse<List<AdminCouponResponse>>.SuccessResponse(list, $"{list.Count} coupon(s) found.");
        }
        catch (Exception ex)
        {
            return ApiResponse<List<AdminCouponResponse>>.ErrorResponse($"Error retrieving coupons: {ex.Message}");
        }
    }

    public async Task<ApiResponse<AdminCouponResponse>> GetCouponByIdAsync(int couponId)
    {
        try
        {
            var row = await _couponRepository.GetCouponByIdAdminAsync(couponId);
            if (row == null)
                return ApiResponse<AdminCouponResponse>.ErrorResponse($"Coupon #{couponId} not found.");

            var now = DateTime.Now;
            return ApiResponse<AdminCouponResponse>.SuccessResponse(
                MapToResponse(row.Value.Coupon, row.Value.DiscountType, row.Value.OrderCount, now));
        }
        catch (Exception ex)
        {
            return ApiResponse<AdminCouponResponse>.ErrorResponse($"Error retrieving coupon: {ex.Message}");
        }
    }

    public async Task<ApiResponse<AdminCouponResponse>> CreateCouponAsync(CreateCouponRequest request)
    {
        try
        {
            if (request.EndDate <= request.StartDate)
                return ApiResponse<AdminCouponResponse>.ErrorResponse("End date must be after start date.");

            if (request.DiscountType == "PERCENTAGE" && request.DiscountValue > 100)
                return ApiResponse<AdminCouponResponse>.ErrorResponse("Percentage discount value cannot exceed 100.");

            var codeUpper = request.Code.ToUpper().Trim();
            if (await _couponRepository.CodeExistsAsync(codeUpper))
                return ApiResponse<AdminCouponResponse>.ErrorResponse($"Coupon code '{codeUpper}' already exists.");

            var id = await _couponRepository.CreateCouponAsync(
                codeUpper, request.DiscountType, request.DiscountValue,
                request.MinOrderValue, request.MaxDiscountAmount,
                request.StartDate, request.EndDate, request.UsageLimit);

            var row = await _couponRepository.GetCouponByIdAdminAsync(id);
            if (row == null)
                return ApiResponse<AdminCouponResponse>.ErrorResponse("Coupon created but could not be retrieved.");

            var now = DateTime.Now;
            return ApiResponse<AdminCouponResponse>.SuccessResponse(
                MapToResponse(row.Value.Coupon, row.Value.DiscountType, row.Value.OrderCount, now),
                "Coupon created successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<AdminCouponResponse>.ErrorResponse($"Error creating coupon: {ex.Message}");
        }
    }

    public async Task<ApiResponse<AdminCouponResponse>> UpdateCouponAsync(int couponId, UpdateCouponRequest request)
    {
        try
        {
            var existing = await _couponRepository.GetCouponByIdAdminAsync(couponId);
            if (existing == null)
                return ApiResponse<AdminCouponResponse>.ErrorResponse($"Coupon #{couponId} not found.");

            if (request.EndDate <= request.StartDate)
                return ApiResponse<AdminCouponResponse>.ErrorResponse("End date must be after start date.");

            if (request.DiscountType == "PERCENTAGE" && request.DiscountValue > 100)
                return ApiResponse<AdminCouponResponse>.ErrorResponse("Percentage discount value cannot exceed 100.");

            var codeUpper = request.Code.ToUpper().Trim();
            if (await _couponRepository.CodeExistsAsync(codeUpper, couponId))
                return ApiResponse<AdminCouponResponse>.ErrorResponse($"Coupon code '{codeUpper}' already used by another coupon.");

            await _couponRepository.UpdateCouponAsync(
                couponId, codeUpper, request.DiscountType, request.DiscountValue,
                request.MinOrderValue, request.MaxDiscountAmount,
                request.StartDate, request.EndDate, request.UsageLimit);

            var updated = await _couponRepository.GetCouponByIdAdminAsync(couponId);
            var now = DateTime.Now;
            return ApiResponse<AdminCouponResponse>.SuccessResponse(
                MapToResponse(updated!.Value.Coupon, updated.Value.DiscountType, updated.Value.OrderCount, now),
                "Coupon updated successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<AdminCouponResponse>.ErrorResponse($"Error updating coupon: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> DeleteCouponAsync(int couponId)
    {
        try
        {
            var existing = await _couponRepository.GetCouponByIdAdminAsync(couponId);
            if (existing == null)
                return ApiResponse<bool>.ErrorResponse($"Coupon #{couponId} not found.");

            if (existing.Value.OrderCount > 0)
                return ApiResponse<bool>.ErrorResponse(
                    $"Cannot delete coupon '{existing.Value.Coupon.Code}' — it has been used in {existing.Value.OrderCount} order(s).");

            await _couponRepository.DeleteCouponAsync(couponId);
            return ApiResponse<bool>.SuccessResponse(true, $"Coupon '{existing.Value.Coupon.Code}' deleted successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResponse($"Error deleting coupon: {ex.Message}");
        }
    }

    private static AdminCouponResponse MapToResponse(Coupon coupon, string discountType, int orderCount, DateTime now)
    {
        var isActive = coupon.StartDate <= now && coupon.EndDate >= now
            && (!coupon.UsageLimit.HasValue || (coupon.UsedCount ?? 0) < coupon.UsageLimit.Value);

        return new AdminCouponResponse
        {
            CouponId = coupon.CouponId,
            Code = coupon.Code,
            DiscountType = discountType,
            DiscountValue = coupon.DiscountValue,
            MinOrderValue = coupon.MinOrderValue,
            MaxDiscountAmount = coupon.MaxDiscountAmount,
            StartDate = coupon.StartDate,
            EndDate = coupon.EndDate,
            UsageLimit = coupon.UsageLimit,
            UsedCount = coupon.UsedCount ?? 0,
            CreatedAt = coupon.CreatedAt,
            OrderCount = orderCount,
            IsActive = isActive,
        };
    }
}
