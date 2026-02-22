using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.DTOs.Admin.Product.Request;
using MV.DomainLayer.DTOs.RequestModels;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.Entities;
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
            var query = _context.Products
                .AsNoTracking()
                .Where(p => p.IsDeleted != true)
                .Include(p => p.Brand)
                .Include(p => p.Categories)
                .Include(p => p.ProductImages)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) ||
                                         p.Sku.ToLower().Contains(term));
            }

            if (filter.BrandId.HasValue)
                query = query.Where(p => p.BrandId == filter.BrandId.Value);

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(p => p.Categories.Any(c => c.CategoryId == filter.CategoryId.Value));
            }

            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<Product> Items, int TotalCount)> GetAdminPagedProductsAsync(AdminProductFilter filter)
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.Categories)
                .Include(p => p.ProductImages)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.Search))
            {
                var term = filter.Search.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) ||
                                         p.Sku.ToLower().Contains(term));
            }

            if (filter.BrandId.HasValue)
                query = query.Where(p => p.BrandId == filter.BrandId.Value);

            if (filter.CategoryId.HasValue)
                query = query.Where(p => p.Categories.Any(c => c.CategoryId == filter.CategoryId.Value));

            if (!string.IsNullOrEmpty(filter.ProductType))
                query = query.Where(p => p.ProductType == filter.ProductType);

            if (filter.LowStock == true)
                query = query.Where(p => (p.StockQuantity ?? 0) < 10);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.StockQuantity ?? 0)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task SoftDeleteAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.IsActive = false;
                product.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
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
            return await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Categories)
                .Include(p => p.ProductImages)
                .Include(p => p.WarrantyPolicy)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public async Task<Product> CreateAsync(Product product)
        {
            product.CreatedAt = DateTime.UtcNow;
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
            return await _context.Products.AnyAsync(p => p.ProductId == productId);
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
