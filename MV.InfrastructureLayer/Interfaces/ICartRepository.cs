using MV.DomainLayer.Entities;

namespace MV.DomainLayer.Interfaces
{
    public interface ICartRepository
    {
        Task<Cart> GetCartByUserIdAsync(int userId);
        Task<CartItem> GetCartItemByIdAsync(int cartItemId);
        Task<Cart> CreateCartAsync(int userId);
        Task<CartItem> AddOrUpdateItemAsync(int cartId, int productId, int quantity);
        Task UpdateCartItemAsync(CartItem cartItem);
        Task DeleteCartItemAsync(CartItem cartItem);
        Task ClearCartAsync(int cartId);
    }
}