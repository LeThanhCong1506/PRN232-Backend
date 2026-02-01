using MV.DomainLayer.DTOs.RequestModels;
using MV.DomainLayer.DTOs.ResponseModels;
using System.Threading.Tasks;

namespace MV.ApplicationLayer.Interfaces
{
    public interface ICartService
    {
        Task<ApiResponse<CartResponseDto>> GetCartAsync(int userId);
        Task<ApiResponse<object>> AddToCartAsync(int userId, AddToCartRequestDto request);
        Task<ApiResponse<object>> UpdateCartItemQuantityAsync(int userId, int cartItemId, int quantity);
        Task<ApiResponse<object>> RemoveCartItemAsync(int userId, int cartItemId);
        Task<ApiResponse<object>> ClearCartAsync(int userId);
        Task<ApiResponse<ValidateCouponResponseDto>> ValidateCouponAsync(int userId, ValidateCouponRequestDto request);
    }
}