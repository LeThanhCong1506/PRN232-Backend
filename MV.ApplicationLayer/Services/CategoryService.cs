using Microsoft.EntityFrameworkCore;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Category.Request;
using MV.DomainLayer.DTOs.Category.Response;
using MV.DomainLayer.DTOs.RequestModels;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly StemDbContext _context;

    public CategoryService(ICategoryRepository categoryRepository, StemDbContext context)
    {
        _categoryRepository = categoryRepository;
        _context = context;
    }

    public async Task<PagedResponse<CategoryResponse>> GetAllCategoriesAsync(PaginationFilter filter)
    {
        try
        {
            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = filter.PageSize < 1 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);

            var categories = await _categoryRepository.GetAllCategoriesAsync(pageNumber, pageSize);
            var totalRecords = await _categoryRepository.GetTotalCategoriesCountAsync();

            var categoryResponses = categories.Select(c => new CategoryResponse
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                ImageUrl = c.ImageUrl,
                ProductCount = c.Products.Count
            }).ToList();

            return new PagedResponse<CategoryResponse>(categoryResponses, pageNumber, pageSize, totalRecords);
        }
        catch (Exception ex)
        {
            return new PagedResponse<CategoryResponse>(new List<CategoryResponse>(), 1, 10, 0);
        }
    }

    public async Task<ApiResponse<CategoryDetailResponse>> GetCategoryByIdAsync(int id)
    {
        try
        {
            var category = await _categoryRepository.GetCategoryWithDetailsAsync(id);

            if (category == null)
            {
                return ApiResponse<CategoryDetailResponse>.ErrorResponse($"Category with ID {id} not found.");
            }

            var categoryDetail = new CategoryDetailResponse
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                ImageUrl = category.ImageUrl,
                TotalProducts = category.Products.Count,
                InStockProducts = category.Products.Count(p => p.StockQuantity > 0),
                AveragePrice = category.Products.Any() ? category.Products.Average(p => p.Price) : null,
                MinPrice = category.Products.Any() ? category.Products.Min(p => p.Price) : null,
                MaxPrice = category.Products.Any() ? category.Products.Max(p => p.Price) : null
            };

            return ApiResponse<CategoryDetailResponse>.SuccessResponse(categoryDetail, "Category details retrieved successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<CategoryDetailResponse>.ErrorResponse($"Error retrieving category: {ex.Message}.");
        }
    }

    public async Task<ApiResponse<CategoryResponse>> CreateCategoryAsync(CreateCategoryRequest request)
    {
        try
        {
            // Check if category name already exists
            var nameExists = await _categoryRepository.CategoryNameExistsAsync(request.Name);
            if (nameExists)
            {
                return ApiResponse<CategoryResponse>.ErrorResponse($"Category with name '{request.Name}' already exists.");
            }

            var category = new Category
            {
                Name = request.Name,
                ImageUrl = request.ImageUrl
            };

            var createdCategory = await _categoryRepository.CreateCategoryAsync(category);

            var categoryResponse = new CategoryResponse
            {
                CategoryId = createdCategory.CategoryId,
                Name = createdCategory.Name,
                ImageUrl = createdCategory.ImageUrl,
                ProductCount = 0
            };

            return ApiResponse<CategoryResponse>.SuccessResponse(categoryResponse, "Category created successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<CategoryResponse>.ErrorResponse($"Error creating category: {ex.Message}.");
        }
    }

    public async Task<ApiResponse<CategoryResponse>> UpdateCategoryAsync(int id, UpdateCategoryRequest request)
    {
        try
        {
            // Check if category exists
            var category = await _categoryRepository.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return ApiResponse<CategoryResponse>.ErrorResponse($"Category with ID {id} not found.");
            }

            // Check if new name already exists (excluding current category)
            var nameExists = await _categoryRepository.CategoryNameExistsAsync(request.Name, id);
            if (nameExists)
            {
                return ApiResponse<CategoryResponse>.ErrorResponse($"Category with name '{request.Name}' already exists.");
            }

            category.Name = request.Name;
            category.ImageUrl = request.ImageUrl;

            await _categoryRepository.UpdateCategoryAsync(category);

            var categoryResponse = new CategoryResponse
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                ImageUrl = category.ImageUrl,
                ProductCount = category.Products.Count
            };

            return ApiResponse<CategoryResponse>.SuccessResponse(categoryResponse, "Category updated successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<CategoryResponse>.ErrorResponse($"Error updating category: {ex.Message}.");
        }
    }

    public async Task<ApiResponse<bool>> DeleteCategoryAsync(int id)
    {
        try
        {
            var category = await _categoryRepository.GetCategoryWithDetailsAsync(id);
            if (category == null)
            {
                return ApiResponse<bool>.ErrorResponse($"Category with ID {id} not found.");
            }

            // Hard delete: remove all rows from the product_category junction table first
            // to avoid FK constraint violations, then delete the category itself.
            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM product_category WHERE category_id = {0}", id);

            await _categoryRepository.DeleteCategoryAsync(id);

            return ApiResponse<bool>.SuccessResponse(true, "Category deleted successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResponse($"Error deleting category: {ex.Message}");
        }
    }
}


