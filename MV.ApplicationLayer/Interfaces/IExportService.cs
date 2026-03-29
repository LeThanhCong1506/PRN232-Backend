using MV.DomainLayer.DTOs.Admin.Order.Request;
using MV.DomainLayer.DTOs.Admin.Product.Request;

namespace MV.ApplicationLayer.Interfaces;

public interface IExportService
{
    Task<byte[]> ExportOrdersAsync(AdminOrderFilter filter);
    Task<byte[]> ExportProductsAsync(AdminProductFilter filter);
    Task<byte[]> ExportUsersAsync();
}
