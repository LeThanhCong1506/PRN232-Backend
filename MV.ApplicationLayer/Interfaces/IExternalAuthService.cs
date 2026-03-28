using MV.DomainLayer.DTOs.Login.Response;
using System.Threading.Tasks;

namespace MV.ApplicationLayer.Interfaces
{
    public interface IExternalAuthService
    {
        Task<LoginResponseDto> GoogleLoginAsync(string code, string redirectUri);
        Task<LoginResponseDto> GitHubLoginAsync(string code, string redirectUri);
    }
}
