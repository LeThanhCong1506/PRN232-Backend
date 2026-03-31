using System.Data;
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

        // ─── Customer-facing ────────────────────────────────────────────────

        public async Task<Coupon?> GetCouponByCodeAsync(string code)
        {
            return await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code.ToLower() == code.ToLower());
        }

        public async Task<string?> GetCouponDiscountTypeAsync(int couponId)
        {
            var result = await _context.Database
                .SqlQueryRaw<string>("SELECT discount_type::text AS \"Value\" FROM coupon WHERE coupon_id = {0}", couponId)
                .FirstOrDefaultAsync();
            return result;
        }

        // ─── Admin CRUD ──────────────────────────────────────────────────────

        public async Task<List<(Coupon Coupon, string DiscountType, int OrderCount)>> GetAllCouponsAsync()
        {
            var results = new List<(Coupon, string, int)>();
            var connection = _context.Database.GetDbConnection();
            var wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen) await connection.OpenAsync();
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT c.coupon_id, c.code, c.discount_type::text,
                           c.discount_value, c.min_order_value, c.max_discount_amount,
                           c.start_date, c.end_date, c.usage_limit, c.used_count, c.created_at,
                           COUNT(oh.order_id)::int AS order_count
                    FROM coupon c
                    LEFT JOIN order_header oh ON oh.coupon_id = c.coupon_id
                    GROUP BY c.coupon_id
                    ORDER BY c.created_at DESC NULLS LAST";

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var coupon = MapCouponFromReader(reader);
                    var discountType = reader.GetString(2);
                    var orderCount = reader.IsDBNull(11) ? 0 : reader.GetInt32(11);
                    results.Add((coupon, discountType, orderCount));
                }
            }
            finally
            {
                if (!wasOpen) await connection.CloseAsync();
            }
            return results;
        }

        public async Task<(Coupon Coupon, string DiscountType, int OrderCount)?> GetCouponByIdAdminAsync(int couponId)
        {
            var connection = _context.Database.GetDbConnection();
            var wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen) await connection.OpenAsync();
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT c.coupon_id, c.code, c.discount_type::text,
                           c.discount_value, c.min_order_value, c.max_discount_amount,
                           c.start_date, c.end_date, c.usage_limit, c.used_count, c.created_at,
                           COUNT(oh.order_id)::int AS order_count
                    FROM coupon c
                    LEFT JOIN order_header oh ON oh.coupon_id = c.coupon_id
                    WHERE c.coupon_id = @id
                    GROUP BY c.coupon_id";

                var p = cmd.CreateParameter();
                p.ParameterName = "@id";
                p.Value = couponId;
                cmd.Parameters.Add(p);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var coupon = MapCouponFromReader(reader);
                    var discountType = reader.GetString(2);
                    var orderCount = reader.IsDBNull(11) ? 0 : reader.GetInt32(11);
                    return (coupon, discountType, orderCount);
                }
                return null;
            }
            finally
            {
                if (!wasOpen) await connection.CloseAsync();
            }
        }

        public async Task<int> CreateCouponAsync(string code, string discountType, decimal discountValue,
            decimal? minOrderValue, decimal? maxDiscountAmount,
            DateTime startDate, DateTime endDate, int? usageLimit)
        {
            var connection = _context.Database.GetDbConnection();
            var wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen) await connection.OpenAsync();
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO coupon (code, discount_type, discount_value, min_order_value, max_discount_amount, start_date, end_date, usage_limit, used_count)
                    VALUES (@code, @discountType::discount_type_enum, @discountValue, @minOrderValue, @maxDiscountAmount, @startDate, @endDate, @usageLimit, 0)
                    RETURNING coupon_id";

                AddParam(cmd, "@code", code.ToUpper());
                AddParam(cmd, "@discountType", discountType);
                AddParam(cmd, "@discountValue", discountValue);
                AddParam(cmd, "@minOrderValue", (object?)minOrderValue ?? DBNull.Value);
                AddParam(cmd, "@maxDiscountAmount", (object?)maxDiscountAmount ?? DBNull.Value);
                AddParam(cmd, "@startDate", startDate);
                AddParam(cmd, "@endDate", endDate);
                AddParam(cmd, "@usageLimit", (object?)usageLimit ?? DBNull.Value);

                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            finally
            {
                if (!wasOpen) await connection.CloseAsync();
            }
        }

        public async Task UpdateCouponAsync(int couponId, string code, string discountType, decimal discountValue,
            decimal? minOrderValue, decimal? maxDiscountAmount,
            DateTime startDate, DateTime endDate, int? usageLimit)
        {
            var connection = _context.Database.GetDbConnection();
            var wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen) await connection.OpenAsync();
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    UPDATE coupon
                    SET code = @code,
                        discount_type = @discountType::discount_type_enum,
                        discount_value = @discountValue,
                        min_order_value = @minOrderValue,
                        max_discount_amount = @maxDiscountAmount,
                        start_date = @startDate,
                        end_date = @endDate,
                        usage_limit = @usageLimit
                    WHERE coupon_id = @couponId";

                AddParam(cmd, "@code", code.ToUpper());
                AddParam(cmd, "@discountType", discountType);
                AddParam(cmd, "@discountValue", discountValue);
                AddParam(cmd, "@minOrderValue", (object?)minOrderValue ?? DBNull.Value);
                AddParam(cmd, "@maxDiscountAmount", (object?)maxDiscountAmount ?? DBNull.Value);
                AddParam(cmd, "@startDate", startDate);
                AddParam(cmd, "@endDate", endDate);
                AddParam(cmd, "@usageLimit", (object?)usageLimit ?? DBNull.Value);
                AddParam(cmd, "@couponId", couponId);

                await cmd.ExecuteNonQueryAsync();
            }
            finally
            {
                if (!wasOpen) await connection.CloseAsync();
            }
        }

        public async Task DeleteCouponAsync(int couponId)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM coupon WHERE coupon_id = {0}", couponId);
        }

        public async Task<bool> CodeExistsAsync(string code, int? excludeCouponId = null)
        {
            var upperCode = code.ToUpper();
            return excludeCouponId.HasValue
                ? await _context.Coupons.AnyAsync(c => c.Code.ToUpper() == upperCode && c.CouponId != excludeCouponId.Value)
                : await _context.Coupons.AnyAsync(c => c.Code.ToUpper() == upperCode);
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static Coupon MapCouponFromReader(System.Data.Common.DbDataReader reader)
        {
            return new Coupon
            {
                CouponId = reader.GetInt32(0),
                Code = reader.GetString(1),
                // index 2 = discount_type (read separately)
                DiscountValue = reader.GetDecimal(3),
                MinOrderValue = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                MaxDiscountAmount = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                StartDate = reader.GetDateTime(6),
                EndDate = reader.GetDateTime(7),
                UsageLimit = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                UsedCount = reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
                CreatedAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
            };
        }

        private static void AddParam(System.Data.Common.DbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }
    }
}
