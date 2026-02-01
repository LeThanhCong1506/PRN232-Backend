using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Interfaces;
using MV.InfrastructureLayer.DBContext;

namespace MV.InfrastructureLayer.Repositories
{
    public class CouponRepository : ICouponRepository
    {
        private readonly StemDbContext _context;

        public CouponRepository(StemDbContext context)
        {
            _context = context;
        }

        public async Task<Coupon?> GetCouponByCodeAsync(string code)
        {
            return await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code.ToLower() == code.ToLower());
        }
    }
}
