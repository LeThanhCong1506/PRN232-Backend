using MV.DomainLayer.Entities;

namespace MV.DomainLayer.Interfaces
{
    public interface ICouponRepository
    {
        // Customer-facing
        Task<Coupon?> GetCouponByCodeAsync(string code);
        /// <summary>
        /// Lấy discount_type của coupon (PostgreSQL enum không được scaffold vào entity).
        /// Trả về "PERCENTAGE" hoặc "FIXED_AMOUNT".
        /// </summary>
        Task<string?> GetCouponDiscountTypeAsync(int couponId);

        // Admin CRUD
        Task<List<(Coupon Coupon, string DiscountType, int OrderCount)>> GetAllCouponsAsync();
        Task<(Coupon Coupon, string DiscountType, int OrderCount)?> GetCouponByIdAdminAsync(int couponId);
        Task<int> CreateCouponAsync(string code, string discountType, decimal discountValue,
            decimal? minOrderValue, decimal? maxDiscountAmount,
            DateTime startDate, DateTime endDate, int? usageLimit);
        Task UpdateCouponAsync(int couponId, string code, string discountType, decimal discountValue,
            decimal? minOrderValue, decimal? maxDiscountAmount,
            DateTime startDate, DateTime endDate, int? usageLimit);
        Task DeleteCouponAsync(int couponId);
        Task<bool> CodeExistsAsync(string code, int? excludeCouponId = null);
    }
}
