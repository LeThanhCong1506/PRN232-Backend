using MV.DomainLayer.Entities;

namespace MV.InfrastructureLayer.Interfaces;

public interface IWarrantyClaimRepository
{
    Task<WarrantyClaim?> GetByIdAsync(int claimId);
    Task<WarrantyClaim> CreateAsync(WarrantyClaim claim);
    Task UpdateAsync(WarrantyClaim claim);

    /// <summary>
    /// Admin: lấy danh sách claims có phân trang, filter theo status
    /// </summary>
    Task<List<WarrantyClaim>> GetAllAsync(string? status, int page, int pageSize);
    Task<int> CountAsync(string? status);
}
