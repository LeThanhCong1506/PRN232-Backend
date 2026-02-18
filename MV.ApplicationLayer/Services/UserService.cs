using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Login.Request;
using MV.DomainLayer.DTOs.Login.Response;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace MV.ApplicationLayer.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        private readonly IJwtService _jwt;

        public UserService(IUserRepository repo, IJwtService jwt)
        {
            _repo = repo;
            _jwt = jwt;
        }

        public async Task<string> CreateAsync(CreateUserDto dto)
        {
            if (await _repo.ExistsUsernameAsync(dto.Username))
                return "The username already exists.";

            if (await _repo.ExistsEmailAsync(dto.Email))
                return "The email already exists.";

            if (await _repo.ExistsPhoneAsync(dto.Phone!))
                return "The phone number already exists.";

            var user = new User
            {
                RoleId = 2, //User role
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = HashPassword(dto.Password),
                Phone = dto.Phone,
                Address = dto.Address,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            await _repo.AddAsync(user);
            return "OK";
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto dto)
        {
            var user = await _repo.GetByEmailAsync(dto.Email);
            if (user == null) return null;

            var hash = HashPassword(dto.Password);
            if (user.PasswordHash != hash) return null;

            return new LoginResponseDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.RoleName,
                AccessToken = _jwt.GenerateToken(user)
            };
        }

        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public async Task<List<UserDto>> GetAllAsync()
        {
            var users = await _repo.GetAllAsync();

            return users.Select(u => new UserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                RoleName = u.Role.RoleName,
                Phone = u.Phone,
                Address = u.Address,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            }).ToList();
        }

        public async Task<UserDto?> GetByIdAsync(int id)
        {
            var user = await _repo.GetByIdAsync(id);
            if (user == null) return null;

            return new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                RoleName = user.Role.RoleName,
                Phone = user.Phone,
                Address = user.Address,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<bool> UpdateAsync(int id, UpdateUserDto dto)
        {
            var user = await _repo.GetByIdAsync(id);
            if (user == null) return false;
            user.Email = dto.Email;
            user.Phone = dto.Phone;
            user.Address = dto.Address;
            await _repo.UpdateAsync(user);
            return true;
        }
    }
}
