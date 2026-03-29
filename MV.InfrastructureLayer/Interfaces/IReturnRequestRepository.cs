using MV.DomainLayer.Entities;

namespace MV.InfrastructureLayer.Interfaces;

public interface IReturnRequestRepository
{
    Task<ReturnRequest> CreateAsync(ReturnRequest request);
    Task<ReturnRequest?> GetByIdAsync(int returnRequestId);
    Task<(List<ReturnRequest> Items, int TotalCount)> GetByUserIdPagedAsync(int userId, int page, int pageSize);
    Task<(List<ReturnRequest> Items, int TotalCount)> GetAllPagedAsync(int page, int pageSize);
    Task UpdateAsync(ReturnRequest request);
    Task<bool> HasActiveRequestForOrderAsync(int orderId);
}
