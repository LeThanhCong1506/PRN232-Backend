using MV.DomainLayer.DTOs.Login.Request;
using MV.DomainLayer.DTOs.Login.Response;
using MV.DomainLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MV.ApplicationLayer.Interfaces
{
    public interface IUserService
    {
        Task<string> CreateAsync(CreateUserDto dto);
        Task<LoginResponseDto?> LoginAsync(LoginDto dto);
        Task<List<UserDto>> GetAllAsync();
        Task<UserDto?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(int id, UpdateUserDto dto);
    }
}
