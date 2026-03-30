using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Admin.Product.Request;
using MV.DomainLayer.DTOs.Product.Request;
using MV.DomainLayer.DTOs.ResponseModels;
using Swashbuckle.AspNetCore.Annotations;

namespace MV.PresentationLayer.Controllers;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "Admin")]
public class AdminProductController : ControllerBase
{
    private readonly IAdminProductService _adminProductService;

    public AdminProductController(IAdminProductService adminProductService)
    {
        _adminProductService = adminProductService;
    }

    /// <summary>
    /// Get all products for admin (includes inactive/deleted)
    /// </summary>
    [HttpGet]
    [SwaggerOperation(Summary = "[Admin] Get all products with admin filters")]
    public async Task<IActionResult> GetProducts([FromQuery] AdminProductFilter filter)
    {
        var result = await _adminProductService.GetAdminProductsAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    [SwaggerOperation(Summary = "[Admin] Create new product")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _adminProductService.CreateProductAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetProducts), new { id = result.Data!.ProductId }, result);
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    [HttpPut("{id}")]
    [SwaggerOperation(Summary = "[Admin] Update product")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _adminProductService.UpdateProductAsync(id, request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Soft delete a product (deactivate)
    /// </summary>
    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "[Admin] Soft delete product")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var result = await _adminProductService.SoftDeleteProductAsync(id);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Toggle product active/inactive status
    /// </summary>
    [HttpPatch("{id}/toggle-active")]
    [SwaggerOperation(Summary = "[Admin] Toggle product active status")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var result = await _adminProductService.ToggleProductActiveAsync(id);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Upload product images (multipart/form-data)
    /// </summary>
    [HttpPost("{id}/images")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(Summary = "[Admin] Upload product images")]
    public async Task<IActionResult> UploadImages(int id, [FromForm] List<IFormFile> files, [FromForm] int? setPrimaryIndex)
    {
        var result = await _adminProductService.UploadImagesAsync(id, files, setPrimaryIndex);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Delete a product image
    /// </summary>
    [HttpDelete("{id}/images/{imageId}")]
    [SwaggerOperation(Summary = "[Admin] Delete product image")]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        var result = await _adminProductService.DeleteImageAsync(id, imageId);

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
                return NotFound(result);
            return BadRequest(result);
        }

        return Ok(result);
    }
}
