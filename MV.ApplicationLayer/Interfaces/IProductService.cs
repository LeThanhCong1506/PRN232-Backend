using MV.DomainLayer.DTOs.RequestModels;
using MV.DomainLayer.DTOs.ResponseModels;

namespace MV.ApplicationLayer.Interfaces
{
    public interface IProductService
    {
        Task<ApiResponse<PagedResponse<ProductResponseDto>>> GetProductsAsync(ProductFilter filter);
    }
}
