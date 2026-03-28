using MV.DomainLayer.Entities;

namespace MV.DomainLayer.Interfaces
{
    public interface ICouponRepository
    {
        Task<Coupon?> GetCouponByCodeAsync(string code);
        /// <summary>
        /// Lấy discount_type của coupon (PostgreSQL enum không được scaffold vào entity).
        /// Trả về "PERCENTAGE" hoặc "FIXED".
        /// </summary>
        Task<string?> GetCouponDiscountTypeAsync(int couponId);
    }
}
