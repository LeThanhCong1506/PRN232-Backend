using MV.DomainLayer.Entities;

namespace MV.ApplicationLayer.Interfaces
{
    public interface IRoleService
    {
        Task<List<Role>> GetAllAsync();
        Task<Role?> GetByIdAsync(int id);
        Task CreateAsync(Role role);
        Task<bool> UpdateAsync(int id, Role role);
        Task<bool> DeleteAsync(int id);
    }
}
