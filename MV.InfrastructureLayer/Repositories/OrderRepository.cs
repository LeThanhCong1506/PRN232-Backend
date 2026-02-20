using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MV.DomainLayer.DTOs.Order.Request;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;
using Npgsql;

namespace MV.InfrastructureLayer.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly StemDbContext _context;

    public OrderRepository(StemDbContext context)
    {
        _context = context;
    }

    // ==================== CREATE ====================

    public async Task<OrderHeader> CreateOrderAsync(OrderHeader orderHeader)
    {
        _context.OrderHeaders.Add(orderHeader);
        await _context.SaveChangesAsync();
        return orderHeader;
    }

    public async Task CreateOrderItemsAsync(List<OrderItem> items)
    {
        _context.OrderItems.AddRange(items);
        await _context.SaveChangesAsync();
    }

    public async Task<Payment> CreatePaymentAsync(Payment payment, string paymentMethod = "COD", string status = "PENDING")
    {
        // INSERT bằng raw SQL vì cột payment_method và status là PostgreSQL enum
        // EF Core không scaffold được enum type → không có property trên entity
        // Dùng NpgsqlParameter để handle NULL values đúng cách
        var conn = _context.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        try
        {
            using var cmd = conn.CreateCommand();

            // Enlist trong transaction hiện tại (nếu có)
            var currentTransaction = _context.Database.CurrentTransaction;
            if (currentTransaction != null)
            {
                cmd.Transaction = currentTransaction.GetDbTransaction();
            }

            cmd.CommandText = @"
                INSERT INTO payment (
                    order_id, amount, payment_date, created_at,
                    transaction_id, bank_code, bank_account, gateway_response,
                    expired_at, payment_reference, updated_at,
                    received_amount, qr_code_url, qr_code_data,
                    retry_count, notes, verified_by, verified_at,
                    payment_method, status
                ) VALUES (
                    @order_id, @amount, @payment_date, @created_at,
                    @transaction_id, @bank_code, @bank_account, @gateway_response,
                    @expired_at, @payment_reference, @updated_at,
                    @received_amount, @qr_code_url, @qr_code_data,
                    @retry_count, @notes, @verified_by, @verified_at,
                    @payment_method::payment_method_enum, @status::payment_status_enum
                ) RETURNING payment_id";

            cmd.Parameters.Add(new NpgsqlParameter("@order_id", payment.OrderId));
            cmd.Parameters.Add(new NpgsqlParameter("@amount", payment.Amount));
            cmd.Parameters.Add(new NpgsqlParameter("@payment_date", (object?)payment.PaymentDate ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@created_at", (object?)payment.CreatedAt ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@transaction_id", (object?)payment.TransactionId ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@bank_code", (object?)payment.BankCode ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@bank_account", (object?)payment.BankAccount ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@gateway_response", (object?)payment.GatewayResponse ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@expired_at", (object?)payment.ExpiredAt ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@payment_reference", (object?)payment.PaymentReference ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@updated_at", (object?)payment.UpdatedAt ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@received_amount", (object?)payment.ReceivedAmount ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@qr_code_url", (object?)payment.QrCodeUrl ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@qr_code_data", (object?)payment.QrCodeData ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@retry_count", (object?)payment.RetryCount ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@notes", (object?)payment.Notes ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@verified_by", (object?)payment.VerifiedBy ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@verified_at", (object?)payment.VerifiedAt ?? DBNull.Value));
            cmd.Parameters.Add(new NpgsqlParameter("@payment_method", paymentMethod));
            cmd.Parameters.Add(new NpgsqlParameter("@status", status));

            var result = await cmd.ExecuteScalarAsync();
            if (result != null)
            {
                payment.PaymentId = Convert.ToInt32(result);
            }
        }
        finally
        {
            // Không đóng connection vì đang trong transaction do EF Core quản lý
        }

        return payment;
    }

    // ==================== READ ====================

    public async Task<OrderHeader?> GetOrderByIdAsync(int orderId)
    {
        return await _context.OrderHeaders
            .Include(o => o.OrderItems)
            .Include(o => o.Payment)
            .Include(o => o.Coupon)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }

    public async Task<OrderHeader?> GetOrderByOrderNumberAsync(string orderNumber)
    {
        return await _context.OrderHeaders
            .Include(o => o.OrderItems)
            .Include(o => o.Payment)
            .Include(o => o.Coupon)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<(List<OrderHeader> Items, int TotalCount)> GetOrdersByUserIdAsync(
        int userId, OrderFilterRequest filter)
    {
        var query = _context.OrderHeaders
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderItems)
            .Include(o => o.Payment)
            .AsQueryable();

        query = ApplyBaseFilters(query, filter);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<OrderHeader> Items, int TotalCount)> GetAllOrdersAsync(OrderFilterRequest filter)
    {
        var query = _context.OrderHeaders
            .Include(o => o.OrderItems)
            .Include(o => o.Payment)
            .AsQueryable();

        query = ApplyBaseFilters(query, filter);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<int> GetTodayOrderCountAsync()
    {
        var today = DateTime.Now.Date;
        return await _context.OrderHeaders
            .CountAsync(o => o.CreatedAt.HasValue && o.CreatedAt.Value.Date == today);
    }

    // ==================== UPDATE ====================

    public async Task UpdateOrderAsync(OrderHeader orderHeader)
    {
        _context.OrderHeaders.Update(orderHeader);
        await _context.SaveChangesAsync();
    }

    public async Task UpdatePaymentAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
    }

    // ==================== STOCK ====================

    public async Task<int> DecrementStockAsync(int productId, int quantity)
    {
        var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
            "UPDATE product SET stock_quantity = stock_quantity - {0} WHERE product_id = {1} AND stock_quantity >= {0}",
            quantity, productId);
        return rowsAffected;
    }

    public async Task IncrementStockAsync(int productId, int quantity)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE product SET stock_quantity = stock_quantity + {0} WHERE product_id = {1}",
            quantity, productId);
    }

    // ==================== COUPON ====================

    public async Task IncrementCouponUsedCountAsync(int couponId)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE coupon SET used_count = COALESCE(used_count, 0) + 1 WHERE coupon_id = {0}",
            couponId);
    }

    public async Task DecrementCouponUsedCountAsync(int couponId)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE coupon SET used_count = GREATEST(COALESCE(used_count, 0) - 1, 0) WHERE coupon_id = {0}",
            couponId);
    }

    public async Task<Coupon?> GetCouponByCodeAsync(string code)
    {
        return await _context.Coupons
            .FirstOrDefaultAsync(c => c.Code.ToLower() == code.ToLower());
    }

    // ==================== RAW SQL FOR ENUM COLUMNS ====================

    public async Task<string?> GetOrderStatusAsync(int orderId)
    {
        var conn = _context.Database.GetDbConnection();
        await conn.OpenAsync();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT status::text FROM order_header WHERE order_id = @id";
            cmd.Parameters.Add(new NpgsqlParameter("@id", orderId));
            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString();
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    public async Task SetOrderStatusAsync(int orderId, string status)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE order_header SET status = {0}::order_status_enum WHERE order_id = {1}",
            status, orderId);
    }

    public async Task<Dictionary<int, string>> GetOrderStatusesBatchAsync(List<int> orderIds)
    {
        var result = new Dictionary<int, string>();
        if (!orderIds.Any()) return result;

        var conn = _context.Database.GetDbConnection();
        await conn.OpenAsync();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT order_id, status::text FROM order_header WHERE order_id = ANY(@ids)";
            cmd.Parameters.Add(new NpgsqlParameter("@ids", orderIds.ToArray()));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var orderId = reader.GetInt32(0);
                var status = reader.IsDBNull(1) ? "PENDING" : reader.GetString(1);
                result[orderId] = status;
            }
        }
        finally
        {
            await conn.CloseAsync();
        }
        return result;
    }

    public async Task<string?> GetPaymentMethodByOrderIdAsync(int orderId)
    {
        var conn = _context.Database.GetDbConnection();
        await conn.OpenAsync();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT p.payment_method::text FROM payment p WHERE p.order_id = @id";
            cmd.Parameters.Add(new NpgsqlParameter("@id", orderId));
            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString();
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    public async Task<string?> GetPaymentStatusByOrderIdAsync(int orderId)
    {
        var conn = _context.Database.GetDbConnection();
        await conn.OpenAsync();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT p.status::text FROM payment p WHERE p.order_id = @id";
            cmd.Parameters.Add(new NpgsqlParameter("@id", orderId));
            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString();
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    public async Task SetPaymentStatusByOrderIdAsync(int orderId, string status)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE payment SET status = {0}::payment_status_enum WHERE order_id = {1}",
            status, orderId);
    }

    public async Task<Dictionary<int, (string? Method, string? Status)>> GetPaymentEnumsBatchAsync(List<int> orderIds)
    {
        var result = new Dictionary<int, (string? Method, string? Status)>();
        if (!orderIds.Any()) return result;

        var conn = _context.Database.GetDbConnection();
        await conn.OpenAsync();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT order_id, payment_method::text, status::text FROM payment WHERE order_id = ANY(@ids)";
            cmd.Parameters.Add(new NpgsqlParameter("@ids", orderIds.ToArray()));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var orderId = reader.GetInt32(0);
                var method = reader.IsDBNull(1) ? null : reader.GetString(1);
                var status = reader.IsDBNull(2) ? null : reader.GetString(2);
                result[orderId] = (method, status);
            }
        }
        finally
        {
            await conn.CloseAsync();
        }
        return result;
    }

    public async Task<string?> GetCouponDiscountTypeAsync(int couponId)
    {
        var conn = _context.Database.GetDbConnection();
        await conn.OpenAsync();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT discount_type::text FROM coupon WHERE coupon_id = @id";
            cmd.Parameters.Add(new NpgsqlParameter("@id", couponId));
            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString();
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    // ==================== POLLING: GET PENDING SEPAY ORDERS ====================

    public async Task<List<OrderHeader>> GetPendingSepayOrdersAsync()
    {
        // Lấy danh sách order có payment PENDING + SEPAY + chưa hết hạn
        // Dùng raw SQL vì payment_method và status là PostgreSQL enum
        var conn = _context.Database.GetDbConnection();
        await conn.OpenAsync();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT p.order_id
                FROM payment p
                WHERE p.status = 'PENDING'::payment_status_enum
                  AND p.payment_method = 'SEPAY'::payment_method_enum
                  AND (p.expired_at IS NULL OR p.expired_at > NOW())";

            var orderIds = new List<int>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orderIds.Add(reader.GetInt32(0));
            }

            if (orderIds.Count == 0)
                return new List<OrderHeader>();

            return await _context.OrderHeaders
                .Include(o => o.Payment)
                .Where(o => orderIds.Contains(o.OrderId))
                .ToListAsync();
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    // ==================== PRIVATE HELPERS ====================

    private static IQueryable<OrderHeader> ApplyBaseFilters(
        IQueryable<OrderHeader> query, OrderFilterRequest filter)
    {
        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToLower().Contains(term) ||
                (o.CustomerName != null && o.CustomerName.ToLower().Contains(term)) ||
                (o.CustomerPhone != null && o.CustomerPhone.Contains(term)));
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= filter.ToDate.Value);
        }

        return query;
    }
}
