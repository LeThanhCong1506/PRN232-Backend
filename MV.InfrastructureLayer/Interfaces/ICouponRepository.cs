using MV.DomainLayer.Entities;

namespace MV.DomainLayer.Interfaces
{
    public interface ICouponRepository
    {
        Task<Coupon?> GetCouponByCodeAsync(string code);
    }
}
