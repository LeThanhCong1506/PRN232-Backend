using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MV.InfrastructureLayer.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly StemDbContext _context;

        public RoleRepository(StemDbContext context)
        {
            _context = context;
        }

        public async Task<List<Role>> GetAllAsync()
        => await _context.Roles.ToListAsync();

        public async Task<Role?> GetByIdAsync(int id)
            => await _context.Roles.FindAsync(id);

        public async Task AddAsync(Role role)
        {
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Role role)
        {
            _context.Roles.Update(role);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Role role)
        {
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
        }
    }
}
