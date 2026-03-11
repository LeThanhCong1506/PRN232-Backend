using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Category.Request;
using MV.DomainLayer.DTOs.RequestModels;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.InfrastructureLayer.Interfaces;

namespace MV.PresentationLayer.Controllers;

/// <summary>
/// Quản lý danh mục sản phẩm (Sensors, Modules, Boards, Accessories...)
/// </summary>
[ApiController]
[Route("api/categories")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ICategoryRepository _categoryRepository;

    public CategoryController(
        ICategoryService categoryService,
        ICloudinaryService cloudinaryService,
        ICategoryRepository categoryRepository)
    {
        _categoryService = categoryService;
        _cloudinaryService = cloudinaryService;
        _categoryRepository = categoryRepository;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories([FromQuery] PaginationFilter filter)
    {
        var result = await _categoryService.GetAllCategoriesAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryById(int id)
    {
        var result = await _categoryService.GetCategoryByIdAsync(id);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _categoryService.CreateCategoryAsync(request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _categoryService.UpdateCategoryAsync(id, request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var result = await _categoryService.DeleteCategoryAsync(id);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Upload category image via Cloudinary (Admin only)
    /// </summary>
    [HttpPost("{id}/image")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadCategoryImage(int id, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<string>.ErrorResponse("No file provided"));

        var category = await _categoryRepository.GetCategoryByIdAsync(id);
        if (category == null)
            return NotFound(ApiResponse<string>.ErrorResponse("Category not found"));

        try
        {
            var (imageUrl, _) = await _cloudinaryService.UploadImageAsync(file, "categories");
            category.ImageUrl = imageUrl;
            await _categoryRepository.UpdateCategoryAsync(category);

            return Ok(ApiResponse<object>.SuccessResponse(new { imageUrl }, "Image uploaded successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<string>.ErrorResponse($"Upload failed: {ex.Message}"));
        }
    }
}
