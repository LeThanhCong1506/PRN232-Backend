using MV.DomainLayer.DTOs.Category.Request;
using MV.DomainLayer.DTOs.Category.Response;
using MV.DomainLayer.DTOs.RequestModels;
using MV.DomainLayer.DTOs.ResponseModels;

namespace MV.ApplicationLayer.Interfaces;

public interface ICategoryService
{
    Task<PagedResponse<CategoryResponse>> GetAllCategoriesAsync(PaginationFilter filter);
    Task<ApiResponse<CategoryDetailResponse>> GetCategoryByIdAsync(int id);
    Task<ApiResponse<CategoryResponse>> CreateCategoryAsync(CreateCategoryRequest request);
    Task<ApiResponse<CategoryResponse>> UpdateCategoryAsync(int id, UpdateCategoryRequest request);
    Task<ApiResponse<bool>> DeleteCategoryAsync(int id);
}
