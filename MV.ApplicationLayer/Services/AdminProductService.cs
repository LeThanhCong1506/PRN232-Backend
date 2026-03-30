using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Admin.Product.Request;
using MV.DomainLayer.DTOs.Admin.Product.Response;
using MV.DomainLayer.DTOs.Product.Request;
using MV.DomainLayer.DTOs.Product.Response;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Enums;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

public class AdminProductService : IAdminProductService
{
    private readonly IProductRepository _productRepo;
    private readonly IProductImageRepository _imageRepo;
    private readonly IBrandRepository _brandRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IProductBundleRepository _bundleRepo;
    private readonly StemDbContext _context;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
    private const int MaxFilesPerUpload = 5;

    public AdminProductService(
        IProductRepository productRepo,
        IProductImageRepository imageRepo,
        IBrandRepository brandRepo,
        ICategoryRepository categoryRepo,
        ICloudinaryService cloudinaryService,
        IProductBundleRepository bundleRepo,
        StemDbContext context)
    {
        _productRepo = productRepo;
        _imageRepo = imageRepo;
        _brandRepo = brandRepo;
        _categoryRepo = categoryRepo;
        _cloudinaryService = cloudinaryService;
        _bundleRepo = bundleRepo;
        _context = context;
    }

    public async Task<ApiResponse<PagedResponse<AdminProductResponse>>> GetAdminProductsAsync(AdminProductFilter filter)
    {
        var (products, totalCount) = await _productRepo.GetAdminPagedProductsAsync(filter);

        // Load bundle components cho tất cả KIT products trong 1 query
        var kitProductIds = products
            .Where(p => p.ProductType == ProductTypeEnum.KIT.ToString())
            .Select(p => p.ProductId)
            .ToList();

        var allBundles = kitProductIds.Any()
            ? (await _bundleRepo.GetBundleComponentsByProductIdsAsync(kitProductIds))
                .GroupBy(b => b.ParentProductId)
                .ToDictionary(g => g.Key, g => g.ToList())
            : new Dictionary<int, List<ProductBundle>>();

        var items = products.Select(p =>
        {
            var effectiveStock = p.ProductType == ProductTypeEnum.KIT.ToString()
                && allBundles.TryGetValue(p.ProductId, out var comps) && comps.Any()
                ? comps.Min(b => (b.ChildProduct.StockQuantity ?? 0) / (b.Quantity ?? 1))
                : p.StockQuantity ?? 0;

            return new AdminProductResponse
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Sku = p.Sku,
                ProductType = p.ProductType,
                Price = p.Price,
                StockQuantity = effectiveStock,
                IsActive = p.IsActive == true,
                BrandName = p.Brand?.Name,
                Categories = p.Categories.Select(c => c.Name).ToList(),
                PrimaryImage = p.ProductImages
                    .Where(i => i.IsPrimary == true)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault()
                    ?? p.ProductImages.OrderBy(i => i.ImageId).FirstOrDefault()?.ImageUrl,
                LowStock = effectiveStock < 10
            };
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
            CompatibilityInfo = request.CompatibilityInfo,
            IsActive = true,
            IsDeleted = false
        };

        var createdProduct = await _productRepo.CreateAsync(product);

        if (request.CategoryIds.Any())
            await _productRepo.AddCategoriesToProductAsync(createdProduct.ProductId, request.CategoryIds);

        // Save specifications
        if (request.Specifications.Any())
        {
            var specs = request.Specifications.Select(s => new ProductSpecification
            {
                ProductId = createdProduct.ProductId,
                SpecName = s.SpecName,
                SpecValue = s.SpecValue,
                DisplayOrder = s.DisplayOrder
            });
            _context.ProductSpecifications.AddRange(specs);
        }

        // Save documents
        if (request.Documents.Any())
        {
            var docs = request.Documents.Select(d => new ProductDocument
            {
                ProductId = createdProduct.ProductId,
                DocumentType = d.DocumentType,
                Title = d.Title,
                Url = d.Url,
                DisplayOrder = d.DisplayOrder
            });
            _context.ProductDocuments.AddRange(docs);
        }

        if (request.Specifications.Any() || request.Documents.Any())
            await _context.SaveChangesAsync();

        var detail = await _productRepo.GetDetailByIdAsync(createdProduct.ProductId);
        var response = MapToDetailResponse(detail!);

        return ApiResponse<ProductDetailResponse>.SuccessResponse(response, "Product created successfully.");
    }

    public async Task<ApiResponse<ProductDetailResponse>> UpdateProductAsync(int productId, UpdateProductRequest request)
    {
        // --- Validation (outside transaction) ---
        var product = await _context.Products
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.ProductId == productId);

        if (product == null)
            return ApiResponse<ProductDetailResponse>.ErrorResponse($"Product with ID {productId} not found.");

        var skuExists = await _context.Products
            .AnyAsync(p => p.Sku == request.Sku && p.ProductId != productId);
        if (skuExists)
            return ApiResponse<ProductDetailResponse>.ErrorResponse($"Product with SKU '{request.Sku}' already exists.");

        var brandExists = await _context.Brands.AnyAsync(b => b.BrandId == request.BrandId);
        if (!brandExists)
            return ApiResponse<ProductDetailResponse>.ErrorResponse($"Brand with ID {request.BrandId} not found.");

        try
        {
            // Update scalar properties
            product.Name = request.Name;
            product.Sku = request.Sku;
            product.Description = request.Description;
            product.ProductType = request.ProductType.ToString();
            product.Price = request.Price;
            product.StockQuantity = request.StockQuantity;
            product.BrandId = request.BrandId;
            product.WarrantyPolicyId = request.WarrantyPolicyId;
            product.HasSerialTracking = request.HasSerialTracking;
            product.CompatibilityInfo = request.CompatibilityInfo;

            // Update categories (many-to-many)
            product.Categories.Clear();
            if (request.CategoryIds.Any())
            {
                var categories = await _context.Categories
                    .Where(c => request.CategoryIds.Contains(c.CategoryId))
                    .ToListAsync();
                foreach (var category in categories)
                    product.Categories.Add(category);
            }

            // Update specifications (delete-then-insert)
            var existingSpecs = await _context.ProductSpecifications
                .Where(s => s.ProductId == productId).ToListAsync();
            _context.ProductSpecifications.RemoveRange(existingSpecs);

            if (request.Specifications.Any())
            {
                var newSpecs = request.Specifications.Select(s => new ProductSpecification
                {
                    ProductId = productId,
                    SpecName = s.SpecName,
                    SpecValue = s.SpecValue,
                    DisplayOrder = s.DisplayOrder
                });
                _context.ProductSpecifications.AddRange(newSpecs);
            }

            // Update documents (delete-then-insert)
            var existingDocs = await _context.ProductDocuments
                .Where(d => d.ProductId == productId).ToListAsync();
            _context.ProductDocuments.RemoveRange(existingDocs);

            if (request.Documents.Any())
            {
                var newDocs = request.Documents.Select(d => new ProductDocument
                {
                    ProductId = productId,
                    DocumentType = d.DocumentType,
                    Title = d.Title,
                    Url = d.Url,
                    DisplayOrder = d.DisplayOrder
                });
                _context.ProductDocuments.AddRange(newDocs);
            }

            // SaveChangesAsync tự wrap tất cả trong 1 transaction — không cần BeginTransactionAsync
            await _context.SaveChangesAsync();

            var detail = await _productRepo.GetDetailByIdAsync(productId);
            var response = MapToDetailResponse(detail!);
            return ApiResponse<ProductDetailResponse>.SuccessResponse(response, "Product updated successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<ProductDetailResponse>.ErrorResponse($"An error occurred while updating the database. Please try again. Details: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> SoftDeleteProductAsync(int productId)
    {
        if (!await _productRepo.ExistsAsync(productId))
            return ApiResponse<bool>.ErrorResponse($"Product with ID {productId} not found.");

        if (await _productRepo.HasOrdersAsync(productId))
            return ApiResponse<bool>.ErrorResponse("Cannot delete this product because it has associated orders. The system will retain the information to ensure data integrity.");

        bool isSuccess = await _productRepo.SoftDeleteAsync(productId);
        if (isSuccess)
        {
            return ApiResponse<bool>.SuccessResponse(true, "Product deleted successfully (hidden from display list).");
        }
        
        return ApiResponse<bool>.ErrorResponse("An error occurred while updating the database. Please try again.");
    }

    public async Task<ApiResponse<bool>> ToggleProductActiveAsync(int productId)
    {
        if (!await _productRepo.ExistsAsync(productId))
            return ApiResponse<bool>.ErrorResponse($"Product with ID {productId} not found.");

        var newStatus = await _productRepo.ToggleActiveAsync(productId);
        if (newStatus == null)
            return ApiResponse<bool>.ErrorResponse("An error occurred while updating the database. Please try again.");

        var statusText = newStatus.Value ? "activated" : "deactivated";
        return ApiResponse<bool>.SuccessResponse(newStatus.Value, $"Product {statusText} successfully.");
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
            CompatibilityInfo = product.CompatibilityInfo,
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
            },
            Specifications = product.ProductSpecifications?.Select(s => new ProductDetailResponse.SpecificationInfo
            {
                SpecificationId = s.SpecificationId,
                SpecName = s.SpecName,
                SpecValue = s.SpecValue,
                DisplayOrder = s.DisplayOrder
            }).ToList() ?? new List<ProductDetailResponse.SpecificationInfo>(),
            Documents = product.ProductDocuments?.Select(d => new ProductDetailResponse.DocumentInfo
            {
                DocumentId = d.DocumentId,
                DocumentType = d.DocumentType,
                Title = d.Title,
                Url = d.Url,
                DisplayOrder = d.DisplayOrder
            }).ToList() ?? new List<ProductDetailResponse.DocumentInfo>(),
            RelatedProducts = product.RelatedProducts?.Select(r => new ProductDetailResponse.RelatedProductInfo
            {
                RelatedProductId = r.RelatedProductId,
                ProductId = r.RelatedToProductId,
                Name = r.RelatedToProduct?.Name,
                Sku = r.RelatedToProduct?.Sku,
                Price = r.RelatedToProduct?.Price ?? 0,
                PrimaryImage = r.RelatedToProduct?.ProductImages?.OrderBy(i => i.ImageId).Select(i => i.ImageUrl).FirstOrDefault(),
                RelationType = r.RelationType,
                DisplayOrder = r.DisplayOrder
            }).ToList() ?? new List<ProductDetailResponse.RelatedProductInfo>()
        };
    }
}
