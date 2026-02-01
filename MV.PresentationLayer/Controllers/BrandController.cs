using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Brand.Request;
using MV.DomainLayer.DTOs.RequestModels;

namespace MV.PresentationLayer.Controllers;

/// <summary>
/// Quản lý thương hiệu (Arduino, Raspberry Pi, ESP32...)
/// </summary>
[ApiController]
[Route("api/brands")]
public class BrandController : ControllerBase
{
    private readonly IBrandService _brandService;

    public BrandController(IBrandService brandService)
    {
        _brandService = brandService;
    }

    /// <summary>
    /// Lấy danh sách brands với phân trang
    /// </summary>
    /// <param name="filter">Pagination: pageNumber, pageSize</param>
    /// <returns>Danh sách brands với số lượng sản phẩm</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/brands?pageNumber=1&amp;pageSize=10
    ///     
    /// Response bao gồm:
    /// - BrandId, Name, LogoUrl
    /// - ProductCount: Tổng số sản phẩm của brand
    /// - InStockProducts: Số sản phẩm còn hàng
    /// - AveragePrice, MinPrice, MaxPrice
    /// </remarks>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetBrands([FromQuery] PaginationFilter filter)
    {
        var result = await _brandService.GetAllBrandsAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết 1 brand
    /// </summary>
    /// <param name="id">Brand ID</param>
    /// <returns>Brand details với thống kê sản phẩm</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/brands/1
    ///     
    /// Response chi tiết:
    /// - Thông tin brand
    /// - TotalProducts: Tổng số sản phẩm
    /// - InStockProducts: Số sản phẩm còn hàng
    /// - Giá trung bình, min, max
    /// </remarks>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBrandById(int id)
    {
        var result = await _brandService.GetBrandByIdAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Tạo brand mới (Admin only)
    /// </summary>
    /// <param name="request">Thông tin brand: Name, LogoUrl</param>
    /// <returns>Brand vừa tạo</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/brands
    ///     {
    ///       "name": "Arduino",
    ///       "logoUrl": "https://example.com/logos/arduino.png"
    ///     }
    ///     
    /// Validation:
    /// - Name: Required, 2-100 ký tự, phải unique
    /// - LogoUrl: Optional, max 255 ký tự
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateBrand([FromBody] CreateBrandRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _brandService.CreateBrandAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetBrandById), new { id = result.Data.BrandId }, result);
    }

    /// <summary>
    /// Cập nhật brand (Admin only)
    /// </summary>
    /// <param name="id">Brand ID cần update</param>
    /// <param name="request">Thông tin mới</param>
    /// <returns>Brand sau khi update</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/brands/1
    ///     {
    ///       "name": "Arduino Official",
    ///       "logoUrl": "https://example.com/logos/arduino-new.png"
    ///     }
    ///     
    /// Validation:
    /// - Name phải unique (trừ chính brand này)
    /// </remarks>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateBrand(int id, [FromBody] UpdateBrandRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _brandService.UpdateBrandAsync(id, request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Xóa brand (Admin only)
    /// </summary>
    /// <param name="id">Brand ID cần xóa</param>
    /// <returns>Success hoặc Error message</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     DELETE /api/brands/5
    ///     
    /// Bảo vệ dữ liệu:
    /// - KHÔNG cho phép xóa brand đã có sản phẩm
    /// - Phải xóa hết products của brand trước
    /// </remarks>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteBrand(int id)
    {
        var result = await _brandService.DeleteBrandAsync(id);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
