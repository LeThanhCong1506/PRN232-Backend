using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.DTOs.Warranty.Request;
using MV.DomainLayer.DTOs.Warranty.Response;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

public class WarrantyService : IWarrantyService
{
    private readonly IWarrantyRepository _warrantyRepository;

    public WarrantyService(IWarrantyRepository warrantyRepository)
    {
        _warrantyRepository = warrantyRepository;
    }

    public async Task<ApiResponse<WarrantyResponse>> GetByIdAsync(int id)
    {
        var warranty = await _warrantyRepository.GetByIdAsync(id);

        if (warranty == null)
        {
            return ApiResponse<WarrantyResponse>.ErrorResponse($"Warranty with ID {id} not found.");
        }

        var response = MapToResponse(warranty);
        return ApiResponse<WarrantyResponse>.SuccessResponse(response);
    }

    public async Task<ApiResponse<WarrantyResponse>> GetBySerialNumberAsync(string serialNumber)
    {
        var warranty = await _warrantyRepository.GetBySerialNumberAsync(serialNumber);

        if (warranty == null)
        {
            return ApiResponse<WarrantyResponse>.ErrorResponse($"Warranty with serial number {serialNumber} not found.");
        }

        var response = MapToResponse(warranty);
        return ApiResponse<WarrantyResponse>.SuccessResponse(response);
    }

    public async Task<ApiResponse<IEnumerable<WarrantyResponse>>> GetAllAsync()
    {
        var warranties = await _warrantyRepository.GetAllAsync();
        var response = warranties.Select(MapToResponse);
        return ApiResponse<IEnumerable<WarrantyResponse>>.SuccessResponse(response);
    }

    public async Task<ApiResponse<IEnumerable<WarrantyResponse>>> GetByProductIdAsync(int productId)
    {
        var warranties = await _warrantyRepository.GetByProductIdAsync(productId);
        var response = warranties.Select(MapToResponse);
        return ApiResponse<IEnumerable<WarrantyResponse>>.SuccessResponse(response);
    }

    public async Task<ApiResponse<IEnumerable<WarrantyResponse>>> GetActiveWarrantiesAsync()
    {
        var warranties = await _warrantyRepository.GetActiveWarrantiesAsync();
        var response = warranties.Select(MapToResponse);
        return ApiResponse<IEnumerable<WarrantyResponse>>.SuccessResponse(response);
    }

    public async Task<ApiResponse<IEnumerable<WarrantyResponse>>> GetExpiredWarrantiesAsync()
    {
        var warranties = await _warrantyRepository.GetExpiredWarrantiesAsync();
        var response = warranties.Select(MapToResponse);
        return ApiResponse<IEnumerable<WarrantyResponse>>.SuccessResponse(response);
    }

    public async Task<ApiResponse<WarrantyResponse>> CreateAsync(CreateWarrantyRequest request)
    {
        // Validate serial number exists
        if (await _warrantyRepository.SerialNumberExistsAsync(request.SerialNumber))
        {
            return ApiResponse<WarrantyResponse>.ErrorResponse($"Warranty for serial number {request.SerialNumber} already exists.");
        }

        // Validate dates
        if (request.EndDate <= request.StartDate)
        {
            return ApiResponse<WarrantyResponse>.ErrorResponse("End date must be after start date.");
        }

        var warranty = new Warranty
        {
            SerialNumber = request.SerialNumber,
            WarrantyPolicyId = request.WarrantyPolicyId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = true,
            ActivationDate = DateTime.UtcNow,
            Notes = request.Notes
        };

        var created = await _warrantyRepository.CreateAsync(warranty);
        var result = await _warrantyRepository.GetByIdAsync(created.WarrantyId);
        var response = MapToResponse(result!);

        return ApiResponse<WarrantyResponse>.SuccessResponse(response, "Warranty created successfully.");
    }

    public async Task<ApiResponse<WarrantyResponse>> UpdateAsync(int id, UpdateWarrantyRequest request)
    {
        var warranty = await _warrantyRepository.GetByIdAsync(id);

        if (warranty == null)
        {
            return ApiResponse<WarrantyResponse>.ErrorResponse($"Warranty with ID {id} not found.");
        }

        warranty.EndDate = request.EndDate;
        warranty.IsActive = request.IsActive;
        warranty.Notes = request.Notes;

        await _warrantyRepository.UpdateAsync(warranty);

        var updated = await _warrantyRepository.GetByIdAsync(id);
        var response = MapToResponse(updated!);

        return ApiResponse<WarrantyResponse>.SuccessResponse(response, "Warranty updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        var warranty = await _warrantyRepository.GetByIdAsync(id);

        if (warranty == null)
        {
            return ApiResponse<bool>.ErrorResponse($"Warranty with ID {id} not found.");
        }

        await _warrantyRepository.DeleteAsync(id);
        return ApiResponse<bool>.SuccessResponse(true, "Warranty deleted successfully.");
    }

    public async Task<ApiResponse<WarrantyResponse>> ActivateWarrantyAsync(string serialNumber)
    {
        var warranty = await _warrantyRepository.GetBySerialNumberAsync(serialNumber);

        if (warranty == null)
        {
            return ApiResponse<WarrantyResponse>.ErrorResponse($"Warranty with serial number {serialNumber} not found.");
        }

        if (warranty.IsActive == true)
        {
            return ApiResponse<WarrantyResponse>.ErrorResponse("Warranty is already activated.");
        }

        warranty.IsActive = true;
        warranty.ActivationDate = DateTime.UtcNow;

        await _warrantyRepository.UpdateAsync(warranty);

        var updated = await _warrantyRepository.GetBySerialNumberAsync(serialNumber);
        var response = MapToResponse(updated!);

        return ApiResponse<WarrantyResponse>.SuccessResponse(response, "Warranty activated successfully.");
    }

    public async Task<ApiResponse<IEnumerable<MyWarrantyResponse>>> GetMyWarrantiesAsync(int userId)
    {
        var warranties = await _warrantyRepository.GetWarrantiesByUserIdAsync(userId);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var response = warranties.Select(w =>
        {
            var monthsRemaining = ((w.EndDate.Year - today.Year) * 12) + (w.EndDate.Month - today.Month);
            if (monthsRemaining < 0) monthsRemaining = 0;

            string status;
            if (w.IsActive == true && w.EndDate >= today)
                status = "ACTIVE";
            else if (w.EndDate < today)
                status = "EXPIRED";
            else
                status = "VOID";

            // Lấy ảnh primary, nếu không có thì lấy ảnh đầu tiên
            var primaryImage = w.SerialNumberNavigation?.Product?.ProductImages
                ?.FirstOrDefault(img => img.IsPrimary == true)?.ImageUrl
                ?? w.SerialNumberNavigation?.Product?.ProductImages?.FirstOrDefault()?.ImageUrl;

            return new MyWarrantyResponse
            {
                WarrantyId = w.WarrantyId,
                Product = new MyWarrantyProductInfo
                {
                    ProductId = w.SerialNumberNavigation.Product.ProductId,
                    Name = w.SerialNumberNavigation.Product.Name,
                    Image = primaryImage
                },
                SerialNumber = w.SerialNumber,
                PurchaseDate = w.StartDate,
                ExpiryDate = w.EndDate,
                MonthsRemaining = monthsRemaining,
                Status = status,
                PolicyName = w.WarrantyPolicy.PolicyName
            };
        });

        return ApiResponse<IEnumerable<MyWarrantyResponse>>.SuccessResponse(response);
    }

    private WarrantyResponse MapToResponse(Warranty warranty)
    {
        return new WarrantyResponse
        {
            WarrantyId = warranty.WarrantyId,
            SerialNumber = warranty.SerialNumber,
            WarrantyPolicyId = warranty.WarrantyPolicyId,
            WarrantyPolicyName = warranty.WarrantyPolicy.PolicyName,
            DurationMonths = warranty.WarrantyPolicy.DurationMonths,
            StartDate = warranty.StartDate,
            EndDate = warranty.EndDate,
            IsActive = warranty.IsActive ?? false,
            ActivationDate = warranty.ActivationDate,
            Notes = warranty.Notes,
            CreatedAt = warranty.CreatedAt,
            ProductId = warranty.SerialNumberNavigation.ProductId,
            ProductName = warranty.SerialNumberNavigation.Product.Name,
            ProductSku = warranty.SerialNumberNavigation.Product.Sku
        };
    }
}
