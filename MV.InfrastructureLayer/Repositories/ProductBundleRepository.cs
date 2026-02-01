using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;

namespace MV.InfrastructureLayer.Repositories;

public class ProductBundleRepository : IProductBundleRepository
{
    private readonly StemDbContext _context;

    public ProductBundleRepository(StemDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProductBundle>> GetBundleComponentsAsync(int parentProductId)
    {
        return await _context.ProductBundles
            .Include(pb => pb.ParentProduct)
            .Include(pb => pb.ChildProduct)
            .Where(pb => pb.ParentProductId == parentProductId)
            .OrderBy(pb => pb.CreatedAt)
            .ToListAsync();
    }

    public async Task<ProductBundle?> GetBundleItemAsync(int parentProductId, int childProductId)
    {
        return await _context.ProductBundles
            .Include(pb => pb.ParentProduct)
            .Include(pb => pb.ChildProduct)
            .FirstOrDefaultAsync(pb => pb.ParentProductId == parentProductId && pb.ChildProductId == childProductId);
    }

    public async Task<ProductBundle> AddToBundleAsync(ProductBundle bundle)
    {
        _context.ProductBundles.Add(bundle);
        await _context.SaveChangesAsync();
        return bundle;
    }

    public async Task RemoveFromBundleAsync(int bundleId)
    {
        var bundle = await _context.ProductBundles.FindAsync(bundleId);
        if (bundle != null)
        {
            _context.ProductBundles.Remove(bundle);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsInBundleAsync(int parentProductId, int childProductId)
    {
        return await _context.ProductBundles
            .AnyAsync(pb => pb.ParentProductId == parentProductId && pb.ChildProductId == childProductId);
    }
}
