using Microsoft.EntityFrameworkCore;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.RequestModels;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.DTOs.Product.Request;
using MV.DomainLayer.DTOs.Product.Response;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Enums;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;

namespace MV.ApplicationLayer.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductBundleRepository _bundleRepository;
        private readonly StemDbContext _context;

        public ProductService(
            IProductRepository productRepository,
            IBrandRepository brandRepository,
            ICategoryRepository categoryRepository,
            IProductBundleRepository bundleRepository,
            StemDbContext context)
        {
            _productRepository = productRepository;
            _brandRepository = brandRepository;
            _categoryRepository = categoryRepository;
            _bundleRepository = bundleRepository;
            _context = context;
        }

        public async Task<ApiResponse<PagedResponse<ProductResponseDto>>> GetProductsAsync(ProductFilter filter)
        {
            // 1. Gọi Repository
            var (products, totalCount) = await _productRepository.GetPagedProductsAsync(filter);

            // 2. Map Entity -> DTO
            var productDtos = products.Select(p => new ProductResponseDto
            {
                ProductId = p.ProductId,
                Sku = p.Sku,
                Name = p.Name,
                Price = p.Price,
                StockQuantity = p.StockQuantity ?? 0,
                ProductType = "MODULE", // Xử lý map enum nếu cần
                Brand = p.Brand != null ? new BrandDto
                {
                    BrandId = p.Brand.BrandId,
                    Name = p.Brand.Name
                } : null,
                PrimaryImage = p.ProductImages.OrderBy(i => i.ImageId).FirstOrDefault()?.ImageUrl ?? "/images/no-image.png",
                Categories = p.Categories.Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name
                }).ToList(),
                InStock = (p.StockQuantity ?? 0) > 0
            }).ToList();

            // 3. Tạo PagedResponse (Đây là object chứa items và pagination)
            var pagedData = new PagedResponse<ProductResponseDto>(
                productDtos,
                filter.PageNumber,
                filter.PageSize,
                totalCount
            );

            // 4. Bọc trong ApiResponse và trả về
            return ApiResponse<PagedResponse<ProductResponseDto>>.SuccessResponse(pagedData);
        }

        public async Task<ApiResponse<List<CategoryResponseDto>>> GetAllCategoriesAsync()
        {
            var categories = await _productRepository.GetAllCategoriesWithCountAsync();

            return ApiResponse<List<CategoryResponseDto>>.SuccessResponse(categories);
        }

        public async Task<ApiResponse<List<BrandResponseDto>>> GetAllBrandsAsync()
        {
            var brands = await _productRepository.GetAllBrandsWithCountAsync();
            return ApiResponse<List<BrandResponseDto>>.SuccessResponse(brands);
        }

        public async Task<ApiResponse<ProductDetailResponse>> GetByIdAsync(int productId)
        {
            var product = await _productRepository.GetDetailByIdAsync(productId);

            if (product == null)
            {
                return ApiResponse<ProductDetailResponse>.ErrorResponse($"Product with ID {productId} not found.");
            }

            var response = MapToDetailResponse(product);

            // Nếu product là KIT, load bundle components
            if (product.ProductType == ProductTypeEnum.KIT.ToString())
            {
                var bundles = await _bundleRepository.GetBundleComponentsAsync(productId);
                response.BundleComponents = bundles.Select(b => new ProductDetailResponse.BundleComponentInfo
                {
                    BundleId = b.BundleId,
                    ChildProductId = b.ChildProductId,
                    ChildProductName = b.ChildProduct.Name,
                    ChildProductSku = b.ChildProduct.Sku,
                    ChildProductPrice = b.ChildProduct.Price,
                    Quantity = b.Quantity ?? 1
                }).ToList();
            }

            return ApiResponse<ProductDetailResponse>.SuccessResponse(response, "Product retrieved successfully.");
        }

        public async Task<ApiResponse<ProductDetailResponse>> CreateAsync(CreateProductRequest request)
        {
            // Validation: SKU unique
            if (await _productRepository.SkuExistsAsync(request.Sku))
            {
                return ApiResponse<ProductDetailResponse>.ErrorResponse($"Product with SKU '{request.Sku}' already exists.");
            }

            // Validation: Brand exists
            if (!await _brandRepository.ExistsAsync(request.BrandId))
            {
                return ApiResponse<ProductDetailResponse>.ErrorResponse($"Brand with ID {request.BrandId} not found.");
            }

            // Validation: WarrantyPolicy exists (if provided)
            if (request.WarrantyPolicyId.HasValue)
            {
                // Add warranty policy validation if needed
            }

            // Validation: Categories exist
            if (request.CategoryIds.Any())
            {
                foreach (var categoryId in request.CategoryIds)
                {
                    if (!await _categoryRepository.ExistsAsync(categoryId))
                    {
                        return ApiResponse<ProductDetailResponse>.ErrorResponse($"Category with ID {categoryId} not found.");
                    }
                }
            }

            // Create product
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
                CompatibilityInfo = request.CompatibilityInfo
            };

            var createdProduct = await _productRepository.CreateAsync(product);

            // Add categories
            if (request.CategoryIds.Any())
            {
                await _productRepository.AddCategoriesToProductAsync(createdProduct.ProductId, request.CategoryIds);
            }

            // Add specifications
            if (request.Specifications.Any())
            {
                var specs = request.Specifications.Select(s => new ProductSpecification
                {
                    ProductId = createdProduct.ProductId,
                    SpecName = s.SpecName,
                    SpecValue = s.SpecValue,
                    DisplayOrder = s.DisplayOrder
                });
                await _context.ProductSpecifications.AddRangeAsync(specs);
                await _context.SaveChangesAsync();
            }

            // Add documents
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
                await _context.ProductDocuments.AddRangeAsync(docs);
                await _context.SaveChangesAsync();
            }

            // Get full detail
            var detail = await _productRepository.GetDetailByIdAsync(createdProduct.ProductId);
            var response = MapToDetailResponse(detail!);

            return ApiResponse<ProductDetailResponse>.SuccessResponse(response, "Product created successfully.");
        }

        public async Task<ApiResponse<ProductDetailResponse>> CreateKitAsync(CreateKitRequest request)
        {
            // Validation: SKU unique
            if (await _productRepository.SkuExistsAsync(request.Sku))
            {
                return ApiResponse<ProductDetailResponse>.ErrorResponse($"Product with SKU '{request.Sku}' already exists.");
            }

            // Validation: Brand exists
            if (!await _brandRepository.ExistsAsync(request.BrandId))
            {
                return ApiResponse<ProductDetailResponse>.ErrorResponse($"Brand with ID {request.BrandId} not found.");
            }

            // Validation: All component products exist and are not KITs
            foreach (var component in request.Components)
            {
                var childProduct = await _productRepository.GetByIdAsync(component.ProductId);
                
                if (childProduct == null)
                {
                    return ApiResponse<ProductDetailResponse>.ErrorResponse($"Component product with ID {component.ProductId} not found.");
                }

                if (childProduct.ProductType == ProductTypeEnum.KIT.ToString())
                {
                    return ApiResponse<ProductDetailResponse>.ErrorResponse($"Cannot add KIT product '{childProduct.Name}' as a component. Only MODULE and COMPONENT types are allowed.");
                }
            }

            // Validation: Categories exist
            if (request.CategoryIds.Any())
            {
                foreach (var categoryId in request.CategoryIds)
                {
                    if (!await _categoryRepository.ExistsAsync(categoryId))
                    {
                        return ApiResponse<ProductDetailResponse>.ErrorResponse($"Category with ID {categoryId} not found.");
                    }
                }
            }

            // Validation: No duplicate components
            var duplicates = request.Components
                .GroupBy(c => c.ProductId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Any())
            {
                return ApiResponse<ProductDetailResponse>.ErrorResponse($"Duplicate component product IDs found: {string.Join(", ", duplicates)}");
            }

            // Create KIT product
            var product = new Product
            {
                Name = request.Name,
                Sku = request.Sku,
                Description = request.Description,
                ProductType = ProductTypeEnum.KIT.ToString(),
                Price = request.Price,
                StockQuantity = 0, // KIT stock is calculated from components
                BrandId = request.BrandId,
                WarrantyPolicyId = request.WarrantyPolicyId,
                HasSerialTracking = false
            };

            var createdProduct = await _productRepository.CreateAsync(product);

            // Add categories
            if (request.CategoryIds.Any())
            {
                await _productRepository.AddCategoriesToProductAsync(createdProduct.ProductId, request.CategoryIds);
            }

            // Add components to bundle
            foreach (var component in request.Components)
            {
                var bundle = new ProductBundle
                {
                    ParentProductId = createdProduct.ProductId,
                    ChildProductId = component.ProductId,
                    Quantity = component.Quantity
                };

                await _context.ProductBundles.AddAsync(bundle);
            }

            await _context.SaveChangesAsync();

            // Get full detail
            var detail = await _productRepository.GetDetailByIdAsync(createdProduct.ProductId);
            var response = MapToDetailResponse(detail!);

            return ApiResponse<ProductDetailResponse>.SuccessResponse(response, "KIT created successfully with all components.");
        }

        public async Task<ApiResponse<ProductDetailResponse>> UpdateAsync(int productId, UpdateProductRequest request)
        {
            var product = await _productRepository.GetByIdAsync(productId);

            if (product == null)
            {
                return ApiResponse<ProductDetailResponse>.ErrorResponse($"Product with ID {productId} not found.");
            }

            // Validation: SKU unique (exclude current product)
            if (await _productRepository.SkuExistsAsync(request.Sku, productId))
            {
                return ApiResponse<ProductDetailResponse>.ErrorResponse($"Product with SKU '{request.Sku}' already exists.");
            }

            // Validation: Brand exists
            if (!await _brandRepository.ExistsAsync(request.BrandId))
            {
                return ApiResponse<ProductDetailResponse>.ErrorResponse($"Brand with ID {request.BrandId} not found.");
            }

            // Update product
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

            await _productRepository.UpdateAsync(product);

            // Update categories: Clear and re-add
            var currentCategoryIds = await _productRepository.GetCategoryIdsByProductIdAsync(productId);
            foreach (var categoryId in currentCategoryIds)
            {
                await _productRepository.RemoveCategoryFromProductAsync(productId, categoryId);
            }

            if (request.CategoryIds.Any())
            {
                await _productRepository.AddCategoriesToProductAsync(productId, request.CategoryIds);
            }

            // Update specifications: delete all, re-add
            var existingSpecs = _context.ProductSpecifications.Where(s => s.ProductId == productId);
            _context.ProductSpecifications.RemoveRange(existingSpecs);

            if (request.Specifications.Any())
            {
                var specs = request.Specifications.Select(s => new ProductSpecification
                {
                    ProductId = productId,
                    SpecName = s.SpecName,
                    SpecValue = s.SpecValue,
                    DisplayOrder = s.DisplayOrder
                });
                await _context.ProductSpecifications.AddRangeAsync(specs);
            }

            // Update documents: delete all, re-add
            var existingDocs = _context.ProductDocuments.Where(d => d.ProductId == productId);
            _context.ProductDocuments.RemoveRange(existingDocs);

            if (request.Documents.Any())
            {
                var docs = request.Documents.Select(d => new ProductDocument
                {
                    ProductId = productId,
                    DocumentType = d.DocumentType,
                    Title = d.Title,
                    Url = d.Url,
                    DisplayOrder = d.DisplayOrder
                });
                await _context.ProductDocuments.AddRangeAsync(docs);
            }

            await _context.SaveChangesAsync();

            // Get full detail
            var detail = await _productRepository.GetDetailByIdAsync(productId);
            var response = MapToDetailResponse(detail!);

            return ApiResponse<ProductDetailResponse>.SuccessResponse(response, "Product updated successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int productId)
        {
            if (!await _productRepository.ExistsAsync(productId))
            {
                return ApiResponse<bool>.ErrorResponse($"Product with ID {productId} not found.");
            }

            // Check if product has orders
            if (await _productRepository.HasOrdersAsync(productId))
            {
                return ApiResponse<bool>.ErrorResponse("Cannot delete product that has been ordered.");
            }

            await _productRepository.DeleteAsync(productId);
            return ApiResponse<bool>.SuccessResponse(true, "Product deleted successfully.");
        }

        public async Task<ApiResponse<bool>> AddCategoriesToProductAsync(int productId, List<int> categoryIds)
        {
            if (!await _productRepository.ExistsAsync(productId))
            {
                return ApiResponse<bool>.ErrorResponse($"Product with ID {productId} not found.");
            }

            foreach (var categoryId in categoryIds)
            {
                if (!await _categoryRepository.ExistsAsync(categoryId))
                {
                    return ApiResponse<bool>.ErrorResponse($"Category with ID {categoryId} not found.");
                }
            }

            await _productRepository.AddCategoriesToProductAsync(productId, categoryIds);
            return ApiResponse<bool>.SuccessResponse(true, "Categories added successfully.");
        }

        public async Task<ApiResponse<bool>> RemoveCategoryFromProductAsync(int productId, int categoryId)
        {
            if (!await _productRepository.ExistsAsync(productId))
            {
                return ApiResponse<bool>.ErrorResponse($"Product with ID {productId} not found.");
            }

            await _productRepository.RemoveCategoryFromProductAsync(productId, categoryId);
            return ApiResponse<bool>.SuccessResponse(true, "Category removed successfully.");
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
                AvailableQuantity = product.StockQuantity ?? 0, // Will calculate based on ProductInstances later
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
                Specifications = product.ProductSpecifications.Select(s => new ProductDetailResponse.SpecificationInfo
                {
                    SpecificationId = s.SpecificationId,
                    SpecName = s.SpecName,
                    SpecValue = s.SpecValue,
                    DisplayOrder = s.DisplayOrder
                }).ToList(),
                Documents = product.ProductDocuments.Select(d => new ProductDetailResponse.DocumentInfo
                {
                    DocumentId = d.DocumentId,
                    DocumentType = d.DocumentType,
                    Title = d.Title,
                    Url = d.Url,
                    DisplayOrder = d.DisplayOrder
                }).ToList(),
                RelatedProducts = product.RelatedProducts.Select(r => new ProductDetailResponse.RelatedProductInfo
                {
                    RelatedProductId = r.RelatedProductId,
                    ProductId = r.RelatedToProductId,
                    Name = r.RelatedToProduct.Name,
                    Sku = r.RelatedToProduct.Sku,
                    Price = r.RelatedToProduct.Price,
                    PrimaryImage = r.RelatedToProduct.ProductImages.OrderBy(i => i.ImageId).Select(i => i.ImageUrl).FirstOrDefault(),
                    RelationType = r.RelationType,
                    DisplayOrder = r.DisplayOrder
                }).ToList()
            };
        }

        public async Task<ApiResponse<List<ProductDetailResponse.RelatedProductInfo>>> GetRelatedProductsAsync(int productId)
        {
            if (!await _productRepository.ExistsAsync(productId))
                return ApiResponse<List<ProductDetailResponse.RelatedProductInfo>>.ErrorResponse($"Product {productId} not found.");

            var related = await _context.RelatedProducts
                .AsNoTracking()
                .Where(r => r.ProductId == productId)
                .OrderBy(r => r.DisplayOrder)
                .Include(r => r.RelatedToProduct)
                    .ThenInclude(p => p.ProductImages)
                .Select(r => new ProductDetailResponse.RelatedProductInfo
                {
                    RelatedProductId = r.RelatedProductId,
                    ProductId = r.RelatedToProductId,
                    Name = r.RelatedToProduct.Name,
                    Sku = r.RelatedToProduct.Sku,
                    Price = r.RelatedToProduct.Price,
                    PrimaryImage = r.RelatedToProduct.ProductImages.OrderBy(i => i.ImageId).Select(i => i.ImageUrl).FirstOrDefault(),
                    RelationType = r.RelationType,
                    DisplayOrder = r.DisplayOrder
                })
                .ToListAsync();

            return ApiResponse<List<ProductDetailResponse.RelatedProductInfo>>.SuccessResponse(related);
        }

        public async Task<ApiResponse<bool>> AddRelatedProductAsync(int productId, CreateRelatedProductDto request)
        {
            if (!await _productRepository.ExistsAsync(productId))
                return ApiResponse<bool>.ErrorResponse($"Product {productId} not found.");

            if (!await _productRepository.ExistsAsync(request.RelatedToProductId))
                return ApiResponse<bool>.ErrorResponse($"Related product {request.RelatedToProductId} not found.");

            if (productId == request.RelatedToProductId)
                return ApiResponse<bool>.ErrorResponse("A product cannot be related to itself.");

            var exists = await _context.RelatedProducts.AnyAsync(r =>
                r.ProductId == productId && r.RelatedToProductId == request.RelatedToProductId);

            if (exists)
                return ApiResponse<bool>.ErrorResponse("This related product already exists.");

            var relatedProduct = new RelatedProduct
            {
                ProductId = productId,
                RelatedToProductId = request.RelatedToProductId,
                RelationType = request.RelationType,
                DisplayOrder = request.DisplayOrder
            };

            await _context.RelatedProducts.AddAsync(relatedProduct);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Related product added.");
        }

        public async Task<ApiResponse<bool>> RemoveRelatedProductAsync(int productId, int relatedProductId)
        {
            var entry = await _context.RelatedProducts
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.RelatedProductId == relatedProductId);

            if (entry == null)
                return ApiResponse<bool>.ErrorResponse("Related product entry not found.");

            _context.RelatedProducts.Remove(entry);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Related product removed.");
        }
    }
}