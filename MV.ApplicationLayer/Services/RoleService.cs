using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.Interfaces;

namespace MV.ApplicationLayer.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _repo;

        public RoleService(IRoleRepository repo)
        {
            _repo = repo;
        }

        public Task<List<Role>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<Role?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public async Task CreateAsync(Role role)
        {
            await _repo.AddAsync(role);
        }

        public async Task<bool> UpdateAsync(int id, Role role)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;

            existing.RoleName = role.RoleName;
            await _repo.UpdateAsync(existing);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var role = await _repo.GetByIdAsync(id);
            if (role == null) return false;

            await _repo.DeleteAsync(role);
            return true;
        }
    }
}
