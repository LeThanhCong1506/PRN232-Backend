using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.RequestModels;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.InfrastructureLayer.Interfaces;

namespace MV.ApplicationLayer.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
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
    }
}