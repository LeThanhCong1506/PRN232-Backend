using Microsoft.AspNetCore.Http;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Admin.Product.Request;
using MV.DomainLayer.DTOs.Admin.Product.Response;
using MV.DomainLayer.DTOs.Product.Request;
using MV.DomainLayer.DTOs.Product.Response;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Enums;
using MV.InfrastructureLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

public class AdminProductService : IAdminProductService
{
    private readonly IProductRepository _productRepo;
    private readonly IProductImageRepository _imageRepo;
    private readonly IBrandRepository _brandRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly ICloudinaryService _cloudinaryService;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
    private const int MaxFilesPerUpload = 5;

    public AdminProductService(
        IProductRepository productRepo,
        IProductImageRepository imageRepo,
        IBrandRepository brandRepo,
        ICategoryRepository categoryRepo,
        ICloudinaryService cloudinaryService)
    {
        _productRepo = productRepo;
        _imageRepo = imageRepo;
        _brandRepo = brandRepo;
        _categoryRepo = categoryRepo;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<ApiResponse<PagedResponse<AdminProductResponse>>> GetAdminProductsAsync(AdminProductFilter filter)
    {
        var (products, totalCount) = await _productRepo.GetAdminPagedProductsAsync(filter);

        var items = products.Select(p => new AdminProductResponse
        {
            ProductId = p.ProductId,
            Name = p.Name,
            Sku = p.Sku,
            ProductType = p.ProductType,
            Price = p.Price,
            StockQuantity = p.StockQuantity ?? 0,
            IsActive = p.IsActive == true,
            BrandName = p.Brand?.Name,
            Categories = p.Categories.Select(c => c.Name).ToList(),
            PrimaryImage = p.ProductImages
                .Where(i => i.IsPrimary == true)
                .Select(i => i.ImageUrl)
                .FirstOrDefault()
                ?? p.ProductImages.OrderBy(i => i.ImageId).FirstOrDefault()?.ImageUrl,
            LowStock = (p.StockQuantity ?? 0) < 10
        }).ToList();

        var pagedData = new PagedResponse<AdminProductResponse>(items, filter.PageNumber, filter.PageSize, totalCount);
        return ApiResponse<PagedResponse<AdminProductResponse>>.SuccessResponse(pagedData);
    }

    public async Task<ApiResponse<ProductDetailResponse>> CreateProductAsync(CreateProductRequest request)
    {
        if (await _productRepo.SkuExistsAsync(request.Sku))
            return ApiResponse<ProductDetailResponse>.ErrorResponse($"Product with SKU '{request.Sku}' already exists.");

        if (!await _brandRepo.ExistsAsync(request.BrandId))
            return ApiResponse<ProductDetailResponse>.ErrorResponse($"Brand with ID {request.BrandId} not found.");

        if (request.CategoryIds.Any())
        {
            foreach (var categoryId in request.CategoryIds)
            {
                if (!await _categoryRepo.ExistsAsync(categoryId))
                    return ApiResponse<ProductDetailResponse>.ErrorResponse($"Category with ID {categoryId} not found.");
            }
        }

        var product = new Product
        {
            Name = request.Name,
            Sku = request.Sku,
            Description = request.Description,
            ProductType = request.ProductType.ToString(),
            Price = request.Price,
            StockQuantity = request.HasSerialTracking ? 0 : request.StockQuantity,
            BrandId = request.BrandId,
            WarrantyPolicyId = request.WarrantyPolicyId,
            HasSerialTracking = request.HasSerialTracking,
            IsActive = true,
            IsDeleted = false
        };

        var createdProduct = await _productRepo.CreateAsync(product);

        if (request.CategoryIds.Any())
            await _productRepo.AddCategoriesToProductAsync(createdProduct.ProductId, request.CategoryIds);

        var detail = await _productRepo.GetDetailByIdAsync(createdProduct.ProductId);
        var response = MapToDetailResponse(detail!);

        return ApiResponse<ProductDetailResponse>.SuccessResponse(response, "Product created successfully.");
    }

    public async Task<ApiResponse<ProductDetailResponse>> UpdateProductAsync(int productId, UpdateProductRequest request)
    {
        var product = await _productRepo.GetByIdAsync(productId);
        if (product == null)
            return ApiResponse<ProductDetailResponse>.ErrorResponse($"Product with ID {productId} not found.");

        if (await _productRepo.SkuExistsAsync(request.Sku, productId))
            return ApiResponse<ProductDetailResponse>.ErrorResponse($"Product with SKU '{request.Sku}' already exists.");

        if (!await _brandRepo.ExistsAsync(request.BrandId))
            return ApiResponse<ProductDetailResponse>.ErrorResponse($"Brand with ID {request.BrandId} not found.");

        product.Name = request.Name;
        product.Sku = request.Sku;
        product.Description = request.Description;
        product.ProductType = request.ProductType.ToString();
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.BrandId = request.BrandId;
        product.WarrantyPolicyId = request.WarrantyPolicyId;
        product.HasSerialTracking = request.HasSerialTracking;

        await _productRepo.UpdateAsync(product);

        // Update categories
        var currentCategoryIds = await _productRepo.GetCategoryIdsByProductIdAsync(productId);
        foreach (var categoryId in currentCategoryIds)
            await _productRepo.RemoveCategoryFromProductAsync(productId, categoryId);

        if (request.CategoryIds.Any())
            await _productRepo.AddCategoriesToProductAsync(productId, request.CategoryIds);

        var detail = await _productRepo.GetDetailByIdAsync(productId);
        var response = MapToDetailResponse(detail!);

        return ApiResponse<ProductDetailResponse>.SuccessResponse(response, "Product updated successfully.");
    }

    public async Task<ApiResponse<bool>> SoftDeleteProductAsync(int productId)
    {
        if (!await _productRepo.ExistsAsync(productId))
            return ApiResponse<bool>.ErrorResponse($"Product with ID {productId} not found.");

        await _productRepo.SoftDeleteAsync(productId);
        return ApiResponse<bool>.SuccessResponse(true, "Product deactivated successfully.");
    }

    public async Task<ApiResponse<List<AdminProductImageResponse>>> UploadImagesAsync(
        int productId, List<IFormFile> files, int? setPrimaryIndex)
    {
        if (!await _productRepo.ExistsAsync(productId))
            return ApiResponse<List<AdminProductImageResponse>>.ErrorResponse($"Product with ID {productId} not found.");

        if (files == null || files.Count == 0)
            return ApiResponse<List<AdminProductImageResponse>>.ErrorResponse("No files provided.");

        if (files.Count > MaxFilesPerUpload)
            return ApiResponse<List<AdminProductImageResponse>>.ErrorResponse($"Maximum {MaxFilesPerUpload} files allowed per upload.");

        // Validate all files first
        foreach (var file in files)
        {
            if (file.Length > MaxFileSize)
                return ApiResponse<List<AdminProductImageResponse>>.ErrorResponse($"File '{file.FileName}' exceeds maximum size of 5MB.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return ApiResponse<List<AdminProductImageResponse>>.ErrorResponse($"File '{file.FileName}' has invalid extension. Allowed: jpg, png, webp.");
        }

        // Upload to Cloudinary
        var existingImages = await _imageRepo.GetByProductIdAsync(productId);
        var hasExistingPrimary = existingImages.Any(i => i.IsPrimary == true);

        var uploadedImages = new List<AdminProductImageResponse>();

        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];

            // Upload to Cloudinary
            var (imageUrl, _) = await _cloudinaryService.UploadImageAsync(file, "products");

            var isPrimary = setPrimaryIndex.HasValue
                ? i == setPrimaryIndex.Value
                : !hasExistingPrimary && i == 0;

            if (isPrimary && (hasExistingPrimary || setPrimaryIndex.HasValue))
            {
                await _imageRepo.ClearPrimaryByProductIdAsync(productId);
                hasExistingPrimary = true;
            }

            var productImage = new ProductImage
            {
                ProductId = productId,
                ImageUrl = imageUrl,
                IsPrimary = isPrimary
            };

            var saved = await _imageRepo.AddAsync(productImage);

            uploadedImages.Add(new AdminProductImageResponse
            {
                ImageId = saved.ImageId,
                ImageUrl = saved.ImageUrl,
                IsPrimary = saved.IsPrimary == true
            });
        }

        return ApiResponse<List<AdminProductImageResponse>>.SuccessResponse(uploadedImages, "Images uploaded successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteImageAsync(int productId, int imageId)
    {
        var image = await _imageRepo.GetByIdAsync(imageId);
        if (image == null)
            return ApiResponse<bool>.ErrorResponse($"Image with ID {imageId} not found.");

        if (image.ProductId != productId)
            return ApiResponse<bool>.ErrorResponse("Image does not belong to this product.");

        // Delete from Cloudinary
        var publicId = _cloudinaryService.ExtractPublicIdFromUrl(image.ImageUrl);
        if (!string.IsNullOrEmpty(publicId))
            await _cloudinaryService.DeleteImageAsync(publicId);

        var wasPrimary = image.IsPrimary == true;
        await _imageRepo.DeleteAsync(imageId);

        // If deleted image was primary, set next image as primary
        if (wasPrimary)
        {
            var remainingImages = await _imageRepo.GetByProductIdAsync(productId);
            if (remainingImages.Any())
            {
                await _imageRepo.SetPrimaryAsync(remainingImages.First().ImageId);
            }
        }

        return ApiResponse<bool>.SuccessResponse(true, "Image deleted successfully.");
    }

    private ProductDetailResponse MapToDetailResponse(Product product)
    {
        return new ProductDetailResponse
        {
            ProductId = product.ProductId,
            Sku = product.Sku,
            Name = product.Name,
            Description = product.Description,
            ProductType = Enum.Parse<ProductTypeEnum>(product.ProductType),
            Price = product.Price,
            StockQuantity = product.StockQuantity ?? 0,
            AvailableQuantity = product.StockQuantity ?? 0,
            HasSerialTracking = product.HasSerialTracking ?? false,
            CreatedAt = product.CreatedAt,
            Brand = new ProductDetailResponse.BrandInfo
            {
                BrandId = product.Brand.BrandId,
                Name = product.Brand.Name,
                LogoUrl = product.Brand.LogoUrl
            },
            WarrantyPolicy = product.WarrantyPolicy != null ? new ProductDetailResponse.WarrantyPolicyInfo
            {
                PolicyId = product.WarrantyPolicy.PolicyId,
                PolicyName = product.WarrantyPolicy.PolicyName,
                DurationMonths = product.WarrantyPolicy.DurationMonths,
                Description = product.WarrantyPolicy.Description
            } : null,
            Categories = product.Categories.Select(c => new ProductDetailResponse.CategoryInfo
            {
                CategoryId = c.CategoryId,
                Name = c.Name
            }).ToList(),
            Images = product.ProductImages.Select(img => new ProductDetailResponse.ProductImageInfo
            {
                ImageId = img.ImageId,
                ImageUrl = img.ImageUrl,
                CreatedAt = img.CreatedAt
            }).ToList(),
            Reviews = new ProductDetailResponse.ReviewSummary
            {
                TotalReviews = product.Reviews.Count,
                AverageRating = product.Reviews.Any() ? product.Reviews.Average(r => r.Rating) : 0,
                FiveStarCount = product.Reviews.Count(r => r.Rating == 5),
                FourStarCount = product.Reviews.Count(r => r.Rating == 4),
                ThreeStarCount = product.Reviews.Count(r => r.Rating == 3),
                TwoStarCount = product.Reviews.Count(r => r.Rating == 2),
                OneStarCount = product.Reviews.Count(r => r.Rating == 1)
            }
        };
    }
}
