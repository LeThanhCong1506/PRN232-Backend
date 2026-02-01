using MV.DomainLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MV.InfrastructureLayer.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> ExistsUsernameAsync(string username);
        Task<bool> ExistsEmailAsync(string email);
        Task<bool> ExistsPhoneAsync(string phone);
        Task AddAsync(User user);
        Task<User?> GetByEmailAsync(string email);
        Task<List<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
    }
}
