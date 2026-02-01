using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;

namespace MV.InfrastructureLayer.Repositories;

public class ProductImageRepository : IProductImageRepository
{
    private readonly StemDbContext _context;

    public ProductImageRepository(StemDbContext context)
    {
        _context = context;
    }

    public async Task<ProductImage> AddAsync(ProductImage productImage)
    {
        productImage.CreatedAt = DateTime.UtcNow;
        _context.ProductImages.Add(productImage);
        await _context.SaveChangesAsync();
        return productImage;
    }

    public async Task DeleteAsync(int imageId)
    {
        var image = await GetByIdAsync(imageId);
        if (image != null)
        {
            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<ProductImage>> GetByProductIdAsync(int productId)
    {
        return await _context.ProductImages
            .Where(pi => pi.ProductId == productId)
            .OrderBy(pi => pi.CreatedAt)
            .ToListAsync();
    }

    public async Task<ProductImage?> GetByIdAsync(int imageId)
    {
        return await _context.ProductImages.FirstOrDefaultAsync(pi => pi.ImageId == imageId);
    }

    public async Task<bool> ExistsAsync(int imageId)
    {
        return await _context.ProductImages.AnyAsync(pi => pi.ImageId == imageId);
    }
}
