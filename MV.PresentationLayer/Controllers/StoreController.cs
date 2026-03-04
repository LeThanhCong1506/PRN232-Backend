using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MV.PresentationLayer.Controllers;

/// <summary>
/// Store information API - Cung cấp thông tin cửa hàng cho mobile app (OpenStreetMap, directions).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StoreController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public StoreController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Lấy thông tin vị trí cửa hàng (cho OpenStreetMap trên mobile)
    /// </summary>
    /// <returns>Tọa độ GPS, tên, địa chỉ, thời gian mở cửa</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/store/location
    ///
    /// Mobile app dùng response này để hiển thị marker trên OpenStreetMap
    /// và tính đường đi (directions) từ vị trí hiện tại của user.
    /// </remarks>
    [HttpGet("location")]
    [SwaggerOperation(Summary = "Get store location for OpenStreetMap")]
    public IActionResult GetStoreLocation()
    {
        var storeSection = _configuration.GetSection("Store");

        var response = new
        {
            Name = storeSection["Name"] ?? "STEM Store",
            Address = storeSection["Address"] ?? "Lô E2a-7, Đường D1, Khu Công nghệ cao, P. Long Thạnh Mỹ, TP. Thủ Đức, TP. HCM",
            Latitude = double.Parse(storeSection["Latitude"] ?? "10.8411", CultureInfo.InvariantCulture),
            Longitude = double.Parse(storeSection["Longitude"] ?? "106.8100", CultureInfo.InvariantCulture),
            Phone = storeSection["Phone"] ?? "0123456789",
            Email = storeSection["Email"] ?? "contact@stemstore.vn",
            OpeningHours = new
            {
                Monday = "08:00 - 21:00",
                Tuesday = "08:00 - 21:00",
                Wednesday = "08:00 - 21:00",
                Thursday = "08:00 - 21:00",
                Friday = "08:00 - 21:00",
                Saturday = "09:00 - 18:00",
                Sunday = "Closed"
            }
        };

        return Ok(new { Success = true, Data = response });
    }

    /// <summary>
    /// Lấy tất cả các chi nhánh cửa hàng
    /// </summary>
    [HttpGet("branches")]
    [SwaggerOperation(Summary = "Get all store branches")]
    public IActionResult GetStoreBranches()
    {
        var branches = _configuration.GetSection("Store:Branches").Get<List<StoreBranchConfig>>();

        if (branches == null || branches.Count == 0)
        {
            // Trả về chi nhánh mặc định nếu chưa cấu hình
            branches = new List<StoreBranchConfig>
            {
                new()
                {
                    Name = "STEM Store - Main Branch",
                    Address = "Lô E2a-7, Đường D1, Khu Công nghệ cao, P. Long Thạnh Mỹ, TP. Thủ Đức, TP. HCM",
                    Latitude = 10.8411,
                    Longitude = 106.8100,
                    Phone = "0123456789",
                    IsMainBranch = true
                }
            };
        }

        return Ok(new { Success = true, Data = branches });
    }
}

public class StoreBranchConfig
{
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Phone { get; set; }
    public bool IsMainBranch { get; set; }
}
