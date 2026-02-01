using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.DTOs.ProductImage.Request;
using MV.DomainLayer.DTOs.ProductImage.Response;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

public class ProductImageService : IProductImageService
{
    private readonly IProductImageRepository _imageRepository;
    private readonly IProductRepository _productRepository;

    public ProductImageService(
        IProductImageRepository imageRepository,
        IProductRepository productRepository)
    {
        _imageRepository = imageRepository;
        _productRepository = productRepository;
    }

    public async Task<ApiResponse<ProductImageResponse>> AddImageAsync(int productId, AddProductImageRequest request)
    {
        // Validate product exists
        if (!await _productRepository.ExistsAsync(productId))
        {
            return ApiResponse<ProductImageResponse>.ErrorResponse($"Product with ID {productId} not found");
        }

        var productImage = new ProductImage
        {
            ProductId = productId,
            ImageUrl = request.ImageUrl
        };

        var created = await _imageRepository.AddAsync(productImage);

        var response = new ProductImageResponse
        {
            ImageId = created.ImageId,
            ProductId = created.ProductId,
            ImageUrl = created.ImageUrl,
            CreatedAt = created.CreatedAt
        };

        return ApiResponse<ProductImageResponse>.SuccessResponse(response, "Image added successfully");
    }

    public async Task<ApiResponse<bool>> DeleteImageAsync(int imageId)
    {
        if (!await _imageRepository.ExistsAsync(imageId))
        {
            return ApiResponse<bool>.ErrorResponse($"Image with ID {imageId} not found");
        }

        await _imageRepository.DeleteAsync(imageId);
        return ApiResponse<bool>.SuccessResponse(true, "Image deleted successfully");
    }

    public async Task<ApiResponse<List<ProductImageResponse>>> GetImagesByProductIdAsync(int productId)
    {
        if (!await _productRepository.ExistsAsync(productId))
        {
            return ApiResponse<List<ProductImageResponse>>.ErrorResponse($"Product with ID {productId} not found");
        }

        var images = await _imageRepository.GetByProductIdAsync(productId);

        var response = images.Select(img => new ProductImageResponse
        {
            ImageId = img.ImageId,
            ProductId = img.ProductId,
            ImageUrl = img.ImageUrl,
            CreatedAt = img.CreatedAt
        }).ToList();

        return ApiResponse<List<ProductImageResponse>>.SuccessResponse(response, "Images retrieved successfully");
    }
}
