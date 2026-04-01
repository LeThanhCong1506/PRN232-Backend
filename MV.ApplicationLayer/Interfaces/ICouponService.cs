using MV.DomainLayer.DTOs.Admin.Coupon.Request;
using MV.DomainLayer.DTOs.Admin.Coupon.Response;
using MV.DomainLayer.DTOs.ResponseModels;

namespace MV.ApplicationLayer.Interfaces;

public interface ICouponService
{
    Task<ApiResponse<List<AdminCouponResponse>>> GetAllCouponsAsync();
    Task<ApiResponse<AdminCouponResponse>> GetCouponByIdAsync(int couponId);
    Task<ApiResponse<AdminCouponResponse>> CreateCouponAsync(CreateCouponRequest request);
    Task<ApiResponse<AdminCouponResponse>> UpdateCouponAsync(int couponId, UpdateCouponRequest request);
    Task<ApiResponse<bool>> DeleteCouponAsync(int couponId);
}
