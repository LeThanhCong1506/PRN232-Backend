using MV.DomainLayer.Entities;

namespace MV.InfrastructureLayer.Interfaces;

public interface IProductBundleRepository
{
    Task<IEnumerable<ProductBundle>> GetBundleComponentsAsync(int parentProductId);
    Task<IEnumerable<ProductBundle>> GetBundleComponentsByProductIdsAsync(IEnumerable<int> parentProductIds);
    Task<ProductBundle?> GetBundleItemAsync(int parentProductId, int childProductId);
    Task<ProductBundle> AddToBundleAsync(ProductBundle bundle);
    Task RemoveFromBundleAsync(int bundleId);
    Task<bool> ExistsInBundleAsync(int parentProductId, int childProductId);
}
