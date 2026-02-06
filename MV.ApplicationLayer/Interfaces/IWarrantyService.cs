using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.DTOs.Warranty.Request;
using MV.DomainLayer.DTOs.Warranty.Response;

namespace MV.ApplicationLayer.Interfaces;

public interface IWarrantyService
{
    Task<ApiResponse<WarrantyResponse>> GetByIdAsync(int id);
    Task<ApiResponse<WarrantyResponse>> GetBySerialNumberAsync(string serialNumber);
    Task<ApiResponse<IEnumerable<WarrantyResponse>>> GetAllAsync();
    Task<ApiResponse<IEnumerable<WarrantyResponse>>> GetByProductIdAsync(int productId);
    Task<ApiResponse<IEnumerable<WarrantyResponse>>> GetActiveWarrantiesAsync();
    Task<ApiResponse<IEnumerable<WarrantyResponse>>> GetExpiredWarrantiesAsync();
    Task<ApiResponse<WarrantyResponse>> CreateAsync(CreateWarrantyRequest request);
    Task<ApiResponse<WarrantyResponse>> UpdateAsync(int id, UpdateWarrantyRequest request);
    Task<ApiResponse<bool>> DeleteAsync(int id);
    Task<ApiResponse<WarrantyResponse>> ActivateWarrantyAsync(string serialNumber);
}
