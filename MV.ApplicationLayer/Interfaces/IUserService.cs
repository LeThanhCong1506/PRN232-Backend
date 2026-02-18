using MV.DomainLayer.DTOs.Login.Request;
using MV.DomainLayer.DTOs.Login.Response;

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
