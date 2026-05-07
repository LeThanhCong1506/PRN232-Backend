using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MV.DomainLayer.DTOs.Admin.Order.Request;
using MV.DomainLayer.DTOs.Admin.Order.Response;
using MV.DomainLayer.DTOs.Order.Request;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Helpers;
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
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            var conn = _context.Database.GetDbConnection();
            await _context.Database.OpenConnectionAsync();
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
                        transaction_id, bank_code, gateway_response,
                        expired_at, payment_reference, updated_at,
                        received_amount, qr_code_url,
                        retry_count, notes, verified_by, verified_at,
                        payment_method, status
                    ) VALUES (
                        @order_id, @amount, @payment_date, @created_at,
                        @transaction_id, @bank_code, @gateway_response,
                        @expired_at, @payment_reference, @updated_at,
                        @received_amount, @qr_code_url,
                        @retry_count, @notes, @verified_by, @verified_at,
                        @payment_method::payment_method_enum, @status::payment_status_enum
                    ) RETURNING payment_id";

                cmd.Parameters.Add(new NpgsqlParameter("@order_id", payment.OrderId));
                cmd.Parameters.Add(new NpgsqlParameter("@amount", payment.Amount));
                cmd.Parameters.Add(new NpgsqlParameter("@payment_date", (object?)payment.PaymentDate ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@created_at", (object?)payment.CreatedAt ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@transaction_id", (object?)payment.TransactionId ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@bank_code", (object?)payment.BankCode ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@gateway_response", (object?)payment.GatewayResponse ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@expired_at", (object?)payment.ExpiredAt ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@payment_reference", (object?)payment.PaymentReference ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@updated_at", (object?)payment.UpdatedAt ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@received_amount", (object?)payment.ReceivedAmount ?? DBNull.Value));
                cmd.Parameters.Add(new NpgsqlParameter("@qr_code_url", (object?)payment.QrCodeUrl ?? DBNull.Value));
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
                await _context.Database.CloseConnectionAsync();
            }
        });

        return payment;
    }

    // ==================== READ ====================

    public async Task<OrderHeader?> GetOrderByIdAsync(int orderId)
    {
        return await _context.OrderHeaders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductInstances)
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
        // PERFORMANCE FIX: Tách Count và Load data riêng để tránh duplicate query
        // Bước 1: Build base query KHÔNG có Include (nhẹ hơn)
        var baseQuery = _context.OrderHeaders
            .Where(o => o.UserId == userId)
            .AsQueryable();

        baseQuery = ApplyBaseFilters(baseQuery, filter);

        // Bước 2: Count trên query nhẹ (không Include relationships)
        var totalCount = await baseQuery.CountAsync();

        // Bước 3: Lấy IDs với pagination
        var orderIds = await baseQuery
            .OrderByDescending(o => o.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(o => o.OrderId)
            .ToListAsync();

        if (orderIds.Count == 0)
            return (new List<OrderHeader>(), totalCount);

        // Bước 4: Load full data với AsSplitQuery chỉ cho các IDs đã lọc
        var items = await _context.OrderHeaders
            .AsSingleQuery() // SingleQuery: 1 connection per query, tránh exhaust pool
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductInstances)
            .Include(o => o.Payment)
            .Where(o => orderIds.Contains(o.OrderId))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<OrderHeader> Items, int TotalCount)> GetAllOrdersAsync(OrderFilterRequest filter)
    {
        // PERFORMANCE FIX: Tách Count và Load data riêng để tránh duplicate query
        // Bước 1: Build base query KHÔNG có Include (nhẹ hơn)
        var baseQuery = _context.OrderHeaders.AsQueryable();
        baseQuery = ApplyBaseFilters(baseQuery, filter);

        // Bước 2: Count trên query nhẹ (không Include relationships)
        var totalCount = await baseQuery.CountAsync();

        // Bước 3: Lấy IDs với pagination
        var orderIds = await baseQuery
            .OrderByDescending(o => o.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(o => o.OrderId)
            .ToListAsync();

        if (orderIds.Count == 0)
            return (new List<OrderHeader>(), totalCount);

        // Bước 4: Load full data với AsSplitQuery chỉ cho các IDs đã lọc
        var items = await _context.OrderHeaders
            .AsSingleQuery() // SingleQuery: 1 connection per query, tránh exhaust pool
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductInstances)
            .Include(o => o.Payment)
            .Where(o => orderIds.Contains(o.OrderId))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<int> GetTodayOrderCountAsync()
    {
        var today = DateTimeHelper.VietnamNow().Date;
        return await _context.OrderHeaders
            .CountAsync(o => o.CreatedAt.HasValue && o.CreatedAt.Value.Date == today);
    }

    public async Task<bool> HasUserPurchasedProductAsync(int userId, int productId)
    {
        var result = await _context.Database
            .SqlQueryRaw<int>(@"
                SELECT 1 AS ""Value""
                FROM order_header oh
                JOIN order_item oi ON oh.order_id = oi.order_id
                WHERE oh.user_id = {0}
                  AND oi.product_id = {1}
                  AND oh.status = 'DELIVERED'::order_status_enum
                LIMIT 1", userId, productId)
            .FirstOrDefaultAsync();
        return result == 1;
    }

    public async Task<bool> HasUserUsedCouponAsync(int userId, int couponId)
    {
        // Kiểm tra user đã dùng coupon này trong đơn hàng nào chưa (trừ đơn CANCELLED)
        var result = await _context.Database
            .SqlQueryRaw<int>(@"
                SELECT 1 AS ""Value""
                FROM order_header
                WHERE user_id = {0}
                  AND coupon_id = {1}
                  AND status != 'CANCELLED'::order_status_enum
                LIMIT 1", userId, couponId)
            .FirstOrDefaultAsync();
        return result == 1;
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
        var result = await _context.Database
            .SqlQueryRaw<string>("SELECT status::text AS \"Value\" FROM order_header WHERE order_id = {0}", orderId)
            .FirstOrDefaultAsync();
        return result;
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

        // Use EF Core execution strategy to safely manage connection
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            var conn = _context.Database.GetDbConnection();
            await _context.Database.OpenConnectionAsync();
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
                await _context.Database.CloseConnectionAsync();
            }
        });
        return result;
    }

    public async Task<string?> GetPaymentMethodByOrderIdAsync(int orderId)
    {
        var result = await _context.Database
            .SqlQueryRaw<string>("SELECT p.payment_method::text AS \"Value\" FROM payment p WHERE p.order_id = {0}", orderId)
            .FirstOrDefaultAsync();
        return result;
    }

    public async Task<string?> GetPaymentStatusByOrderIdAsync(int orderId)
    {
        var result = await _context.Database
            .SqlQueryRaw<string>("SELECT p.status::text AS \"Value\" FROM payment p WHERE p.order_id = {0}", orderId)
            .FirstOrDefaultAsync();
        return result;
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

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            var conn = _context.Database.GetDbConnection();
            await _context.Database.OpenConnectionAsync();
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
                await _context.Database.CloseConnectionAsync();
            }
        });
        return result;
    }

    public async Task<string?> GetCouponDiscountTypeAsync(int couponId)
    {
        var result = await _context.Database
            .SqlQueryRaw<string>("SELECT discount_type::text AS \"Value\" FROM coupon WHERE coupon_id = {0}", couponId)
            .FirstOrDefaultAsync();
        return result;
    }

    // ==================== ADMIN ORDER MANAGEMENT ====================

    public async Task<(List<OrderHeader> Items, int TotalCount)> GetAdminOrdersAsync(AdminOrderFilter filter)
    {
        // Build base query with raw SQL for enum filtering
        var conditions = new List<string> { "1=1" };
        var paramValues = new Dictionary<string, object>();
        var paramIndex = 0;

        if (!string.IsNullOrEmpty(filter.Search))
        {
            conditions.Add($"(oh.order_number ILIKE @p{paramIndex} OR oh.customer_name ILIKE @p{paramIndex} OR oh.customer_phone ILIKE @p{paramIndex} OR oh.customer_email ILIKE @p{paramIndex})");
            paramValues[$"@p{paramIndex}"] = $"%{filter.Search}%";
            paramIndex++;
        }

        if (!string.IsNullOrEmpty(filter.Status))
        {
            conditions.Add($"oh.status = @p{paramIndex}::order_status_enum");
            paramValues[$"@p{paramIndex}"] = filter.Status;
            paramIndex++;
        }

        if (!string.IsNullOrEmpty(filter.PaymentMethod))
        {
            conditions.Add($"p.payment_method = @p{paramIndex}::payment_method_enum");
            paramValues[$"@p{paramIndex}"] = filter.PaymentMethod;
            paramIndex++;
        }

        if (!string.IsNullOrEmpty(filter.PaymentStatus))
        {
            conditions.Add($"p.status = @p{paramIndex}::payment_status_enum");
            paramValues[$"@p{paramIndex}"] = filter.PaymentStatus;
            paramIndex++;
        }

        if (filter.DateFrom.HasValue)
        {
            conditions.Add($"oh.created_at >= @p{paramIndex}");
            paramValues[$"@p{paramIndex}"] = filter.DateFrom.Value;
            paramIndex++;
        }

        if (filter.DateTo.HasValue)
        {
            conditions.Add($"oh.created_at <= @p{paramIndex}");
            paramValues[$"@p{paramIndex}"] = filter.DateTo.Value;
            paramIndex++;
        }

        var whereClause = string.Join(" AND ", conditions);

        var totalCount = 0;
        var orderIds = new List<int>();
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            var conn = _context.Database.GetDbConnection();
            await _context.Database.OpenConnectionAsync();
            try
            {
                // Count query
                using var countCmd = conn.CreateCommand();
                countCmd.CommandText = $@"
                    SELECT COUNT(DISTINCT oh.order_id)
                    FROM order_header oh
                    LEFT JOIN payment p ON p.order_id = oh.order_id
                    WHERE {whereClause}";
                foreach (var kv in paramValues)
                    countCmd.Parameters.Add(new NpgsqlParameter(kv.Key, kv.Value));
                totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

                // IDs query with pagination
                using var idsCmd = conn.CreateCommand();
                idsCmd.CommandText = $@"
                    SELECT DISTINCT oh.order_id
                    FROM order_header oh
                    LEFT JOIN payment p ON p.order_id = oh.order_id
                    WHERE {whereClause}
                    ORDER BY oh.order_id DESC
                    OFFSET @offset LIMIT @limit";
                foreach (var kv in paramValues)
                    idsCmd.Parameters.Add(new NpgsqlParameter(kv.Key, kv.Value));
                idsCmd.Parameters.Add(new NpgsqlParameter("@offset", (filter.PageNumber - 1) * filter.PageSize));
                idsCmd.Parameters.Add(new NpgsqlParameter("@limit", filter.PageSize));

                using var reader = await idsCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    orderIds.Add(reader.GetInt32(0));
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        });

        if (orderIds.Count == 0)
            return (new List<OrderHeader>(), totalCount);

        var items = await _context.OrderHeaders
            .AsNoTracking()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductInstances)
            .Include(o => o.Payment)
            .Include(o => o.User)
            .Where(o => orderIds.Contains(o.OrderId))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Dictionary<string, int>> GetOrderStatusCountsAsync()
    {
        var result = new Dictionary<string, int>();
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            var conn = _context.Database.GetDbConnection();
            await _context.Database.OpenConnectionAsync();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT status::text, COUNT(*) FROM order_header GROUP BY status";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var status = reader.IsDBNull(0) ? "UNKNOWN" : reader.GetString(0);
                    var count = reader.GetInt64(1);
                    result[status] = (int)count;
                }
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        });
        return result;
    }

    public async Task<decimal> GetDeliveredRevenueAsync(DateTime from, DateTime to)
    {
        var result = await _context.Database
            .SqlQueryRaw<decimal>(@"
                SELECT COALESCE(SUM(total_amount), 0) AS ""Value""
                FROM order_header
                WHERE status = 'DELIVERED'::order_status_enum
                  AND delivered_at >= {0} AND delivered_at <= {1}", from, to)
            .FirstOrDefaultAsync();
        return result;
    }

    public async Task<List<OrderHeader>> GetRecentOrdersAsync(int count)
    {
        return await _context.OrderHeaders
            .AsNoTracking()
            .Include(o => o.Payment)
            .OrderByDescending(o => o.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<DailyRevenueData>> GetDailyRevenueAsync(DateTime from, DateTime to, string? status = null)
    {
        var result = new List<DailyRevenueData>();
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            var conn = _context.Database.GetDbConnection();
            await _context.Database.OpenConnectionAsync();
            try
            {
                using var cmd = conn.CreateCommand();
                var statusFilter = string.IsNullOrEmpty(status)
                    ? "status != 'CANCELLED'::order_status_enum"
                    : "status = @status::order_status_enum";
                cmd.CommandText = $@"
                    SELECT DATE(created_at) as d, SUM(total_amount) as revenue, COUNT(*) as cnt
                    FROM order_header
                    WHERE {statusFilter}
                      AND created_at >= @from AND created_at <= @to
                    GROUP BY DATE(created_at)
                    ORDER BY DATE(created_at)";
                cmd.Parameters.Add(new NpgsqlParameter("@from", from));
                cmd.Parameters.Add(new NpgsqlParameter("@to", to));
                if (!string.IsNullOrEmpty(status))
                    cmd.Parameters.Add(new NpgsqlParameter("@status", status));
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new DailyRevenueData
                    {
                        Date = reader.GetDateTime(0).ToString("yyyy-MM-dd"),
                        Revenue = reader.GetDecimal(1),
                        OrderCount = (int)reader.GetInt64(2)
                    });
                }
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        });
        return result;
    }

    public async Task SetProductInstanceStatusByOrderItemIdsAsync(List<int> orderItemIds, string status)
    {
        if (!orderItemIds.Any()) return;

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            var conn = _context.Database.GetDbConnection();
            await _context.Database.OpenConnectionAsync();
            try
            {
                using var cmd = conn.CreateCommand();
                var currentTransaction = _context.Database.CurrentTransaction;
                if (currentTransaction != null)
                    cmd.Transaction = currentTransaction.GetDbTransaction();

                cmd.CommandText = @"
                    UPDATE product_instance
                    SET status = @status::instance_status_enum
                    WHERE order_item_id = ANY(@ids)";
                cmd.Parameters.Add(new NpgsqlParameter("@status", status));
                cmd.Parameters.Add(new NpgsqlParameter("@ids", orderItemIds.ToArray()));
                await cmd.ExecuteNonQueryAsync();
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        });
    }

    public async Task CreateWarrantiesForDeliveredOrderAsync(int orderId)
    {
        var order = await _context.OrderHeaders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null) return;

        var today = DateOnly.FromDateTime(DateTime.Today);

        foreach (var orderItem in order.OrderItems)
        {
            var product = orderItem.Product;
            if (product == null || product.WarrantyPolicyId == null) continue;

            var policy = await _context.WarrantyPolicies
                .FirstOrDefaultAsync(wp => wp.PolicyId == product.WarrantyPolicyId.Value);
            if (policy == null) continue;

            // Check if ProductInstance already exists for this order item
            var instances = await _context.ProductInstances
                .Where(pi => pi.OrderItemId == orderItem.OrderItemId)
                .ToListAsync();

            if (instances.Count > 0)
            {
                // Use existing instances
                foreach (var instance in instances)
                {
                    var alreadyHasWarranty = await _context.Warranties
                        .AnyAsync(w => w.SerialNumber == instance.SerialNumber);
                    if (alreadyHasWarranty) continue;

                    _context.Warranties.Add(new Warranty
                    {
                        SerialNumber = instance.SerialNumber,
                        WarrantyPolicyId = policy.PolicyId,
                        StartDate = today,
                        EndDate = today.AddMonths(policy.DurationMonths),
                        IsActive = true,
                        ActivationDate = DateTimeHelper.VietnamNow(),
                        Notes = $"Auto-created on delivery of order #{order.OrderNumber}",
                        CreatedAt = DateTimeHelper.VietnamNow()
                    });
                }
            }
            else
            {
                // No serial tracking — auto-create a ProductInstance for warranty purposes
                for (int i = 0; i < orderItem.Quantity; i++)
                {
                    var serialNumber = $"SN-{orderId}-{orderItem.OrderItemId}-{i + 1}-{DateTimeHelper.VietnamNow():yyyyMMddHHmmss}";

                    var instance = new ProductInstance
                    {
                        SerialNumber = serialNumber,
                        ProductId = product.ProductId,
                        OrderItemId = orderItem.OrderItemId,
                        CreatedAt = DateTimeHelper.VietnamNow()
                    };
                    _context.ProductInstances.Add(instance);

                    // Set status via raw SQL after flush
                    await _context.SaveChangesAsync();

                    // Set status to SOLD using raw SQL (enum column)
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE product_instance SET status = 'SOLD'::instance_status_enum WHERE serial_number = {0}",
                        serialNumber);

                    _context.Warranties.Add(new Warranty
                    {
                        SerialNumber = serialNumber,
                        WarrantyPolicyId = policy.PolicyId,
                        StartDate = today,
                        EndDate = today.AddMonths(policy.DurationMonths),
                        IsActive = true,
                        ActivationDate = DateTimeHelper.VietnamNow(),
                        Notes = $"Auto-created on delivery of order #{order.OrderNumber}",
                        CreatedAt = DateTimeHelper.VietnamNow()
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
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
