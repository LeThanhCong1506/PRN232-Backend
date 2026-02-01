using MV.DomainLayer.DTOs.ProductBundle.Request;
using MV.DomainLayer.DTOs.ProductBundle.Response;
using MV.DomainLayer.DTOs.ResponseModels;

namespace MV.ApplicationLayer.Interfaces;

public interface IProductBundleService
{
    Task<ApiResponse<IEnumerable<ProductBundleResponse>>> GetBundleComponentsAsync(int kitProductId);
    Task<ApiResponse<ProductBundleResponse>> AddProductToBundleAsync(int kitProductId, AddProductToBundleRequest request);
    Task<ApiResponse<bool>> RemoveProductFromBundleAsync(int kitProductId, int childProductId);
    Task<ApiResponse<int>> GetAvailableKitStockAsync(int kitProductId);
}
