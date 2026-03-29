using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Admin.Order.Request;
using MV.DomainLayer.DTOs.Admin.Product.Request;

namespace MV.PresentationLayer.Controllers.Admin;

[Authorize(Roles = "Admin,Staff")]
[Route("api/export")]
[ApiController]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;

    public ExportController(IExportService exportService)
    {
        _exportService = exportService;
    }

    [HttpGet("orders")]
    public async Task<IActionResult> ExportOrders([FromQuery] AdminOrderFilter filter)
    {
        var bytes = await _exportService.ExportOrdersAsync(filter);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Orders_Report.xlsx");
    }

    [HttpGet("products")]
    public async Task<IActionResult> ExportProducts([FromQuery] AdminProductFilter filter)
    {
        var bytes = await _exportService.ExportProductsAsync(filter);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Inventory_Report.xlsx");
    }

    [HttpGet("users")]
    public async Task<IActionResult> ExportUsers()
    {
        var bytes = await _exportService.ExportUsersAsync();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Users_Report.xlsx");
    }
}
