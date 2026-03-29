using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.DTOs.Admin.Product.Request;
using MV.DomainLayer.DTOs.RequestModels;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Helpers;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;

namespace MV.InfrastructureLayer.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly StemDbContext _context;

        public ProductRepository(StemDbContext context)
        {
            _context = context;
        }

        public async Task<(List<Product> Items, int TotalCount)> GetPagedProductsAsync(ProductFilter filter)
        {
            // PERFORMANCE FIX: Tách Count và Load riêng
            // Bước 1: Build base query KHÔNG có Include
            var baseQuery = _context.Products
                .Where(p => p.IsActive == true)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                baseQuery = baseQuery.Where(p => p.Name.ToLower().Contains(term) ||
                                         p.Sku.ToLower().Contains(term));
            }

            if (filter.BrandId.HasValue)
                baseQuery = baseQuery.Where(p => p.BrandId == filter.BrandId.Value);

            if (filter.CategoryId.HasValue)
            {
                baseQuery = baseQuery.Where(p => p.Categories.Any(c => c.CategoryId == filter.CategoryId.Value));
            }

            if (filter.MinPrice.HasValue)
                baseQuery = baseQuery.Where(p => p.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                baseQuery = baseQuery.Where(p => p.Price <= filter.MaxPrice.Value);

            // Bước 2: Count trên query nhẹ
            var totalCount = await baseQuery.CountAsync();

            // Bước 3: Lấy IDs với pagination
            var isAsc = filter.SortOrder?.ToLower() == "asc";
            IQueryable<Product> sortedQuery = filter.SortBy?.ToLower() switch
            {
                "price" => isAsc ? baseQuery.OrderBy(p => p.Price) : baseQuery.OrderByDescending(p => p.Price),
                "name" => isAsc ? baseQuery.OrderBy(p => p.Name) : baseQuery.OrderByDescending(p => p.Name),
                _ => isAsc ? baseQuery.OrderBy(p => p.CreatedAt) : baseQuery.OrderByDescending(p => p.CreatedAt)
            };

            var productIds = await sortedQuery
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => p.ProductId)
                .ToListAsync();

            if (productIds.Count == 0)
                return (new List<Product>(), totalCount);

            // Bước 4: Load full data với AsSplitQuery
            var items = await _context.Products
                .AsNoTracking()
                .AsSingleQuery()
                .Include(p => p.Brand)
                .Include(p => p.Categories)
                .Include(p => p.ProductImages)
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();

            // Giữ đúng thứ tự sort
            items = productIds.Select(id => items.First(p => p.ProductId == id)).ToList();

            return (items, totalCount);
        }

        public async Task<(List<Product> Items, int TotalCount)> GetAdminPagedProductsAsync(AdminProductFilter filter)
        {
            // PERFORMANCE FIX: Tách Count và Load riêng
            // Bước 1: Build base query KHÔNG có Include
            var baseQuery = _context.Products
                .Where(p => p.IsActive == true)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.Search))
            {
                var term = filter.Search.ToLower();
                baseQuery = baseQuery.Where(p => p.Name.ToLower().Contains(term) ||
                                         p.Sku.ToLower().Contains(term));
            }

            if (filter.BrandId.HasValue)
                baseQuery = baseQuery.Where(p => p.BrandId == filter.BrandId.Value);

            if (filter.CategoryId.HasValue)
                baseQuery = baseQuery.Where(p => p.Categories.Any(c => c.CategoryId == filter.CategoryId.Value));

            if (!string.IsNullOrEmpty(filter.ProductType))
                baseQuery = baseQuery.Where(p => p.ProductType == filter.ProductType);

            if (filter.LowStock == true)
                baseQuery = baseQuery.Where(p => (p.StockQuantity ?? 0) < 10);

            // Bước 2: Count trên query nhẹ
            var totalCount = await baseQuery.CountAsync();

            // Bước 3: Lấy IDs với pagination
            var productIds = await baseQuery
                .OrderBy(p => p.StockQuantity ?? 0)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => p.ProductId)
                .ToListAsync();

            if (productIds.Count == 0)
                return (new List<Product>(), totalCount);

            // Bước 4: Load full data với AsSplitQuery
            var items = await _context.Products
                .AsNoTracking()
                .AsSingleQuery()
                .Include(p => p.Brand)
                .Include(p => p.Categories)
                .Include(p => p.ProductImages)
                .Where(p => productIds.Contains(p.ProductId))
                .OrderBy(p => p.StockQuantity ?? 0)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task SoftDeleteAsync(int productId)
        {
            // Dùng ExecuteUpdateAsync để bypass NoTracking global setting
            // Trực tiếp generate SQL: UPDATE product SET is_active = false WHERE product_id = @id
            await _context.Products
                .Where(p => p.ProductId == productId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));
        }

        public async Task<Product> GetProductByIdAsync(int productId)
        {
            return await _context.Products.FindAsync(productId);
        }

        public async Task<List<CategoryResponseDto>> GetAllCategoriesWithCountAsync()
        {
            var query = _context.Categories
                .AsNoTracking()
                .Select(c => new CategoryResponseDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    ProductCount = c.Products.Count()
                });

            return await query.ToListAsync();
        }

        public async Task<List<BrandResponseDto>> GetAllBrandsWithCountAsync()
        {
            var query = _context.Brands
                .AsNoTracking()
                .Select(b => new BrandResponseDto
                {
                    BrandId = b.BrandId,
                    Name = b.Name,
                    LogoUrl = b.LogoUrl ?? "https://www.nosm.ca/wp-content/uploads/2024/01/Photo-placeholder-1024x1024.jpg",
                    ProductCount = b.Products.Count()
                });

            return await query.ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int productId)
        {
            return await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public async Task<Product?> GetDetailByIdAsync(int productId)
        {
            // PERFORMANCE FIX: Tách reviews ra query riêng và limit số lượng
            // Load product với basic includes (không include Reviews)
            var product = await _context.Products
                .AsNoTracking()
                .AsSingleQuery()
                .Include(p => p.Brand)
                .Include(p => p.Categories)
                .Include(p => p.ProductImages)
                .Include(p => p.WarrantyPolicy)
                .Include(p => p.ProductSpecifications.OrderBy(s => s.DisplayOrder))
                .Include(p => p.ProductDocuments.OrderBy(d => d.DisplayOrder))
                .Include(p => p.RelatedProducts.OrderBy(r => r.DisplayOrder))
                    .ThenInclude(r => r.RelatedToProduct)
                        .ThenInclude(rp => rp.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null) return null;

            // Load reviews separately với pagination (tránh load tất cả reviews)
            // Chỉ load 20 reviews gần nhất thay vì toàn bộ
            product.Reviews = await _context.Reviews
                .AsNoTracking()
                .Include(r => r.User)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(20)
                .ToListAsync();

            return product;
        }

        public async Task<Product> CreateAsync(Product product)
        {
            product.CreatedAt = DateTimeHelper.VietnamNow();
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int productId)
        {
            var product = await GetByIdAsync(productId);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int productId)
        {
            // Check IsActive != false to also include products where IsActive is null (not explicitly deactivated)
            return await _context.Products.AnyAsync(p => p.ProductId == productId && p.IsActive != false);
        }

        public async Task<bool> SkuExistsAsync(string sku, int? excludeProductId = null)
        {
            var query = _context.Products.Where(p => p.Sku.ToLower() == sku.ToLower());

            if (excludeProductId.HasValue)
            {
                query = query.Where(p => p.ProductId != excludeProductId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<bool> HasOrdersAsync(int productId)
        {
            return await _context.OrderItems.AnyAsync(oi => oi.ProductId == productId);
        }

        public async Task AddCategoriesToProductAsync(int productId, List<int> categoryIds)
        {
            var product = await _context.Products
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null) return;

            var categories = await _context.Categories
                .Where(c => categoryIds.Contains(c.CategoryId))
                .ToListAsync();

            foreach (var category in categories)
            {
                if (!product.Categories.Any(c => c.CategoryId == category.CategoryId))
                {
                    product.Categories.Add(category);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task RemoveCategoryFromProductAsync(int productId, int categoryId)
        {
            var product = await _context.Products
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null) return;

            var category = product.Categories.FirstOrDefault(c => c.CategoryId == categoryId);
            if (category != null)
            {
                product.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<int>> GetCategoryIdsByProductIdAsync(int productId)
        {
            return await _context.Products
                .Where(p => p.ProductId == productId)
                .SelectMany(p => p.Categories.Select(c => c.CategoryId))
                .ToListAsync();
        }
    }
}
