using Microsoft.EntityFrameworkCore;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Brand.Request;
using MV.DomainLayer.DTOs.Brand.Response;
using MV.DomainLayer.DTOs.RequestModels;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

public class BrandService : IBrandService
{
    private readonly IBrandRepository _brandRepository;
    private readonly StemDbContext _context;

    public BrandService(IBrandRepository brandRepository, StemDbContext context)
    {
        _brandRepository = brandRepository;
        _context = context;
    }

    public async Task<PagedResponse<BrandResponse>> GetAllBrandsAsync(PaginationFilter filter)
    {
        try
        {
            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = filter.PageSize < 1 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);

            var brands = await _brandRepository.GetAllBrandsAsync(pageNumber, pageSize);
            var totalRecords = await _brandRepository.GetTotalBrandsCountAsync();

            var brandResponses = brands.Select(b => new BrandResponse
            {
                BrandId = b.BrandId,
                Name = b.Name,
                LogoUrl = b.LogoUrl,
                ProductCount = b.Products.Count
            }).ToList();

            return new PagedResponse<BrandResponse>(brandResponses, pageNumber, pageSize, totalRecords);
        }
        catch (Exception ex)
        {
            return new PagedResponse<BrandResponse>(new List<BrandResponse>(), 1, 10, 0);
        }
    }

    public async Task<ApiResponse<BrandDetailResponse>> GetBrandByIdAsync(int id)
    {
        try
        {
            var brand = await _brandRepository.GetBrandWithDetailsAsync(id);

            if (brand == null)
            {
                return ApiResponse<BrandDetailResponse>.ErrorResponse($"Brand with ID {id} not found.");
            }

            var brandDetail = new BrandDetailResponse
            {
                BrandId = brand.BrandId,
                Name = brand.Name,
                LogoUrl = brand.LogoUrl,
                TotalProducts = brand.Products.Count,
                InStockProducts = brand.Products.Count(p => p.StockQuantity > 0),
                AveragePrice = brand.Products.Any() ? brand.Products.Average(p => p.Price) : null,
                MinPrice = brand.Products.Any() ? brand.Products.Min(p => p.Price) : null,
                MaxPrice = brand.Products.Any() ? brand.Products.Max(p => p.Price) : null
            };

            return ApiResponse<BrandDetailResponse>.SuccessResponse(brandDetail, "Brand details retrieved successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<BrandDetailResponse>.ErrorResponse($"Error retrieving brand: {ex.Message}");
        }
    }

    public async Task<ApiResponse<BrandResponse>> CreateBrandAsync(CreateBrandRequest request)
    {
        try
        {
            // Check if brand name already exists
            var nameExists = await _brandRepository.BrandNameExistsAsync(request.Name);
            if (nameExists)
            {
                return ApiResponse<BrandResponse>.ErrorResponse($"Brand with name '{request.Name}' already exists.");
            }

            var brand = new Brand
            {
                Name = request.Name,
                LogoUrl = request.LogoUrl
            };

            var createdBrand = await _brandRepository.CreateBrandAsync(brand);

            var brandResponse = new BrandResponse
            {
                BrandId = createdBrand.BrandId,
                Name = createdBrand.Name,
                LogoUrl = createdBrand.LogoUrl,
                ProductCount = 0
            };

            return ApiResponse<BrandResponse>.SuccessResponse(brandResponse, "Brand created successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<BrandResponse>.ErrorResponse($"Error creating brand: {ex.Message}");
        }
    }

    public async Task<ApiResponse<BrandResponse>> UpdateBrandAsync(int id, UpdateBrandRequest request)
    {
        try
        {
            // Check if brand exists
            var brand = await _brandRepository.GetBrandByIdAsync(id);
            if (brand == null)
            {
                return ApiResponse<BrandResponse>.ErrorResponse($"Brand with ID {id} not found.");
            }

            // Check if new name already exists (excluding current brand)
            var nameExists = await _brandRepository.BrandNameExistsAsync(request.Name, id);
            if (nameExists)
            {
                return ApiResponse<BrandResponse>.ErrorResponse($"Brand with name '{request.Name}' already exists.");
            }

            brand.Name = request.Name;
            brand.LogoUrl = request.LogoUrl;

            await _brandRepository.UpdateBrandAsync(brand);

            var brandResponse = new BrandResponse
            {
                BrandId = brand.BrandId,
                Name = brand.Name,
                LogoUrl = brand.LogoUrl,
                ProductCount = brand.Products.Count
            };

            return ApiResponse<BrandResponse>.SuccessResponse(brandResponse, "Brand updated successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<BrandResponse>.ErrorResponse($"Error updating brand: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> DeleteBrandAsync(int id)
    {
        try
        {
            var brand = await _brandRepository.GetBrandWithDetailsAsync(id);
            if (brand == null)
            {
                return ApiResponse<bool>.ErrorResponse($"Brand with ID {id} not found.");
            }

            // Hard delete: BrandId is non-nullable on Product (FK Restrict),
            // so soft-delete all associated products first (IsDeleted=true, IsActive=false),
            // then delete the brand itself.
            if (brand.Products.Any())
            {
                await _context.Products
                    .Where(p => p.BrandId == id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.IsDeleted, true)
                        .SetProperty(p => p.IsActive, false));
            }

            await _brandRepository.DeleteBrandAsync(id);

            return ApiResponse<bool>.SuccessResponse(true, "Brand deleted successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResponse($"Error deleting brand: {ex.Message}");
        }
    }
}
