using ClosedXML.Excel;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Admin.Order.Request;
using MV.DomainLayer.DTOs.Admin.Product.Request;
using System.Data;

namespace MV.ApplicationLayer.Services;

public class ExportService : IExportService
{
    private readonly IAdminOrderService _adminOrderService;
    private readonly IAdminProductService _adminProductService;
    private readonly IUserService _userService;

    public ExportService(
        IAdminOrderService adminOrderService,
        IAdminProductService adminProductService,
        IUserService userService)
    {
        _adminOrderService = adminOrderService;
        _adminProductService = adminProductService;
        _userService = userService;
    }

    public async Task<byte[]> ExportOrdersAsync(AdminOrderFilter filter)
    {
        // Get all matching orders by using a large page size
        filter.PageNumber = 1;
        filter.PageSize = 10000;
        
        var result = await _adminOrderService.GetAdminOrdersAsync(filter);
        var orders = result.Data?.Orders ?? new();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Orders");

        // Headers
        worksheet.Cell(1, 1).Value = "Mã Đơn";
        worksheet.Cell(1, 2).Value = "Ngày Tạo";
        worksheet.Cell(1, 3).Value = "Khách Hàng";
        worksheet.Cell(1, 4).Value = "SĐT";
        worksheet.Cell(1, 5).Value = "Tổng Tiền";
        worksheet.Cell(1, 6).Value = "Phí Ship";
        worksheet.Cell(1, 7).Value = "Giảm Giá";
        worksheet.Cell(1, 8).Value = "Phương thức TT";
        worksheet.Cell(1, 9).Value = "Trạng thái TT";
        worksheet.Cell(1, 10).Value = "Trạng thái Đơn hàng";

        var row = 2;
        foreach (var order in orders)
        {
            worksheet.Cell(row, 1).Value = order.OrderNumber;
            worksheet.Cell(row, 2).Value = order.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss");
            worksheet.Cell(row, 3).Value = order.CustomerName;
            worksheet.Cell(row, 4).Value = order.CustomerPhone;
            worksheet.Cell(row, 5).Value = order.TotalAmount;
            worksheet.Cell(row, 6).Value = order.ShippingFee;
            worksheet.Cell(row, 7).Value = order.DiscountAmount;
            worksheet.Cell(row, 8).Value = order.PaymentMethod;
            worksheet.Cell(row, 9).Value = order.PaymentStatus;
            worksheet.Cell(row, 10).Value = order.Status;
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportProductsAsync(AdminProductFilter filter)
    {
        filter.PageNumber = 1;
        filter.PageSize = 10000;
        
        var result = await _adminProductService.GetAdminProductsAsync(filter);
        var products = result.Data?.Items ?? new();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Inventory");

        // Headers: SKU, Tên Sản Phẩm, Danh Mục, Thương Hiệu, Giá Bán, Số Lượng Tồn Kho, Trạng Thái
        worksheet.Cell(1, 1).Value = "SKU";
        worksheet.Cell(1, 2).Value = "Tên Sản Phẩm";
        worksheet.Cell(1, 3).Value = "Danh Mục";
        worksheet.Cell(1, 4).Value = "Thương Hiệu";
        worksheet.Cell(1, 5).Value = "Giá Bán";
        worksheet.Cell(1, 6).Value = "Số Lượng Tồn Kho";
        worksheet.Cell(1, 7).Value = "Trạng Thái";

        var row = 2;
        foreach (var product in products)
        {
            worksheet.Cell(row, 1).Value = product.Sku;
            worksheet.Cell(row, 2).Value = product.Name;
            worksheet.Cell(row, 3).Value = string.Join(", ", product.Categories);
            worksheet.Cell(row, 4).Value = product.BrandName;
            worksheet.Cell(row, 5).Value = product.Price;
            worksheet.Cell(row, 6).Value = product.StockQuantity;
            worksheet.Cell(row, 7).Value = product.IsActive ? "Active" : "Inactive";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportUsersAsync()
    {
        var users = await _userService.GetAllAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Users");

        // Headers: Tên đăng nhập, Họ & Tên, Email, SĐT, Ngày đăng ký
        worksheet.Cell(1, 1).Value = "Tên đăng nhập";
        worksheet.Cell(1, 2).Value = "Họ & Tên";
        worksheet.Cell(1, 3).Value = "Email";
        worksheet.Cell(1, 4).Value = "SĐT";
        worksheet.Cell(1, 5).Value = "Ngày đăng ký";

        var row = 2;
        foreach (var user in users)
        {
            worksheet.Cell(row, 1).Value = user.Username;
            worksheet.Cell(row, 2).Value = user.FullName;
            worksheet.Cell(row, 3).Value = user.Email;
            worksheet.Cell(row, 4).Value = user.Phone;
            worksheet.Cell(row, 5).Value = user.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss");
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
