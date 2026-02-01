using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.ProductImage.Request;
using Swashbuckle.AspNetCore.Annotations;

namespace MV.PresentationLayer.Controllers;

/// <summary>
/// Quản lý ảnh sản phẩm
/// </summary>
[ApiController]
[Route("api/products")]
public class ProductImageController : ControllerBase
{
    private readonly IProductImageService _imageService;

    public ProductImageController(IProductImageService imageService)
    {
        _imageService = imageService;
    }

    /// <summary>
    /// Thêm ảnh cho sản phẩm (Admin only)
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="request">URL ảnh cần thêm</param>
    /// <returns>Thông tin ảnh vừa thêm</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/products/5/images
    ///     {
    ///       "imageUrl": "https://example.com/images/arduino-uno-front.jpg"
    ///     }
    ///     
    /// Lưu ý:
    /// - Có thể thêm nhiều ảnh cho 1 sản phẩm
    /// - Ảnh đầu tiên sẽ là ảnh chính (PrimaryImage)
    /// - Hỗ trợ URL external hoặc path local
    /// </remarks>
    [HttpPost("{productId}/images")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Add product image")]
    public async Task<IActionResult> AddProductImage(int productId, [FromBody] AddProductImageRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _imageService.AddImageAsync(productId, request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Lấy tất cả ảnh của sản phẩm
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>Danh sách ảnh theo thứ tự thêm vào</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/products/5/images
    ///     
    /// Public endpoint - Không cần authentication
    /// </remarks>
    [HttpGet("{productId}/images")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Get all product images")]
    public async Task<IActionResult> GetProductImages(int productId)
    {
        var result = await _imageService.GetImagesByProductIdAsync(productId);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Xóa ảnh sản phẩm (Admin only)
    /// </summary>
    /// <param name="imageId">Image ID cần xóa</param>
    /// <returns>Success message</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     DELETE /api/products/images/25
    ///     
    /// Lưu ý:
    /// - Không xóa được nếu Image ID không tồn tại
    /// - File ảnh trên server/CDN không tự động xóa (cần xử lý riêng)
    /// </remarks>
    [HttpDelete("images/{imageId}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Delete product image")]
    public async Task<IActionResult> DeleteProductImage(int imageId)
    {
        var result = await _imageService.DeleteImageAsync(imageId);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
