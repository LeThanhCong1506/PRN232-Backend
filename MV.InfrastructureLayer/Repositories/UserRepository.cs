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
    public class UserRepository : IUserRepository
    {
        private readonly StemDbContext _context;

        public UserRepository(StemDbContext context)
        {
            _context = context;
        }

        public Task<bool> ExistsUsernameAsync(string username)
            => _context.Users.AnyAsync(x => x.Username == username);

        public Task<bool> ExistsEmailAsync(string email)
            => _context.Users.AnyAsync(x => x.Email == email);

        public Task<bool> ExistsPhoneAsync(string phone) 
            => _context.Users.AnyAsync(x => x.Phone == phone);

        public async Task AddAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public Task<User?> GetByEmailAsync(string email)
        => _context.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == email && x.IsActive == true);

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(x => x.Role)
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.UserId == id);
        }
    }
}
