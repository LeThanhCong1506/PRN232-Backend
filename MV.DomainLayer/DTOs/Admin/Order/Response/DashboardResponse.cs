namespace MV.DomainLayer.DTOs.Admin.Order.Response;

public class DashboardResponse
{
    public OrderStats Orders { get; set; } = new();
    public ProductStats Products { get; set; } = new();
    public CustomerStats Customers { get; set; } = new();
    public List<RecentOrderDto> RecentOrders { get; set; } = new();

    public class OrderStats
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ConfirmedOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
    }

    public class ProductStats
    {
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
    }

    public class CustomerStats
    {
        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }
    }

    public class RecentOrderDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = null!;
        public string? CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Status { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}

public class DailyRevenueData
{
    public string Date { get; set; } = null!;
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}
