using MV.DomainLayer.DTOs.Brand.Request;
using MV.DomainLayer.DTOs.Brand.Response;
using MV.DomainLayer.DTOs.RequestModels;
using MV.DomainLayer.DTOs.ResponseModels;

namespace MV.ApplicationLayer.Interfaces;

public interface IBrandService
{
    Task<PagedResponse<BrandResponse>> GetAllBrandsAsync(PaginationFilter filter);
    Task<ApiResponse<BrandDetailResponse>> GetBrandByIdAsync(int id);
    Task<ApiResponse<BrandResponse>> CreateBrandAsync(CreateBrandRequest request);
    Task<ApiResponse<BrandResponse>> UpdateBrandAsync(int id, UpdateBrandRequest request);
    Task<ApiResponse<bool>> DeleteBrandAsync(int id);
}
