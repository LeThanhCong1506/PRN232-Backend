using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Category.Request;
using MV.DomainLayer.DTOs.RequestModels;

namespace MV.PresentationLayer.Controllers;

/// <summary>
/// Quản lý danh mục sản phẩm (Sensors, Modules, Boards, Accessories...)
/// </summary>
[ApiController]
[Route("api/categories")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Lấy danh sách categories với phân trang
    /// </summary>
    /// <param name="filter">Pagination: pageNumber, pageSize</param>
    /// <returns>Danh sách categories với số lượng sản phẩm</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/categories?pageNumber=1&amp;pageSize=10
    ///     
    /// Response bao gồm:
    /// - CategoryId, Name
    /// - ProductCount: Tổng số sản phẩm trong category
    /// - InStockProducts: Số sản phẩm còn hàng
    /// - AveragePrice, MinPrice, MaxPrice
    /// </remarks>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories([FromQuery] PaginationFilter filter)
    {
        var result = await _categoryService.GetAllCategoriesAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết 1 category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <returns>Category details với thống kê sản phẩm</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/categories/3
    ///     
    /// Response chi tiết:
    /// - Thông tin category
    /// - TotalProducts: Tổng số sản phẩm
    /// - InStockProducts: Số sản phẩm còn hàng
    /// - Giá trung bình, min, max
    /// </remarks>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryById(int id)
    {
        var result = await _categoryService.GetCategoryByIdAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Tạo category mới (Admin only)
    /// </summary>
    /// <param name="request">Thông tin category: Name</param>
    /// <returns>Category vừa tạo</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/categories
    ///     {
    ///       "name": "Temperature Sensors"
    ///     }
    ///     
    /// Validation:
    /// - Name: Required, 2-100 ký tự, phải unique
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _categoryService.CreateCategoryAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetCategoryById), new { id = result.Data.CategoryId }, result);
    }

    /// <summary>
    /// Cập nhật category (Admin only)
    /// </summary>
    /// <param name="id">Category ID cần update</param>
    /// <param name="request">Thông tin mới</param>
    /// <returns>Category sau khi update</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/categories/3
    ///     {
    ///       "name": "Temperature &amp; Humidity Sensors"
    ///     }
    ///     
    /// Validation:
    /// - Name phải unique (trừ chính category này)
    /// </remarks>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _categoryService.UpdateCategoryAsync(id, request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Xóa category (Admin only)
    /// </summary>
    /// <param name="id">Category ID cần xóa</param>
    /// <returns>Success hoặc Error message</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     DELETE /api/categories/10
    ///     
    /// Bảo vệ dữ liệu:
    /// - KHÔNG cho phép xóa category đã có sản phẩm
    /// - Phải gỡ products khỏi category trước
    /// </remarks>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var result = await _categoryService.DeleteCategoryAsync(id);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
