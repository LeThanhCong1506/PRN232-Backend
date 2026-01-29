using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.DTOs.RequestModels;
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
                .Include(p => p.Brand)
                .Include(p => p.Categories)
                .Include(p => p.ProductImages)
                .AsQueryable();

            // 1. Filter
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

            // 2. Count Total (Trước khi phân trang)
            var totalCount = await query.CountAsync();

            // 3. Paging
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}