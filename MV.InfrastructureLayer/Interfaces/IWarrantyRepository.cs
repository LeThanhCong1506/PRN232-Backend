using MV.DomainLayer.Entities;

namespace MV.InfrastructureLayer.Interfaces;

public interface IWarrantyRepository
{
    Task<Warranty?> GetByIdAsync(int id);
    Task<Warranty?> GetBySerialNumberAsync(string serialNumber);
    Task<IEnumerable<Warranty>> GetAllAsync();
    Task<IEnumerable<Warranty>> GetByProductIdAsync(int productId);
    Task<IEnumerable<Warranty>> GetActiveWarrantiesAsync();
    Task<IEnumerable<Warranty>> GetExpiredWarrantiesAsync();
    Task<Warranty> CreateAsync(Warranty warranty);
    Task UpdateAsync(Warranty warranty);
    Task DeleteAsync(int id);
    Task<bool> SerialNumberExistsAsync(string serialNumber, int? excludeId = null);
}
