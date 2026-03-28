using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Helpers;
using MV.DomainLayer.Interfaces;
using MV.InfrastructureLayer.DBContext;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MV.InfrastructureLayer.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly StemDbContext _context;

        public CartRepository(StemDbContext context)
        {
            _context = context;
        }

        public async Task<Cart> GetCartByUserIdAsync(int userId)
        {
            // PERFORMANCE FIX: Thêm AsSplitQuery cho multiple Include
            return await _context.Carts
                .AsNoTracking()
                .AsSingleQuery()
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.ProductImages) // Include ảnh để hiển thị
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task<CartItem> GetCartItemByIdAsync(int cartItemId)
        {
            return await _context.CartItems
                .Include(ci => ci.Cart)    // Để check quyền sở hữu (UserId)
                .Include(ci => ci.Product) // Để check Stock và tính giá
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);
        }

        public async Task<Cart> CreateCartAsync(int userId)
        {
            var cart = new Cart
            {
                UserId = userId,
                CreatedAt = DateTimeHelper.VietnamNow()
            };

            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
            return cart;
        }

        public async Task<CartItem> AddOrUpdateItemAsync(int cartId, int productId, int quantity)
        {
            // 1. Kiểm tra xem sản phẩm này đã có trong giỏ chưa
            var existingItem = await _context.CartItems
                .Include(ci => ci.Product) // Include Product để lấy giá tính tiền sau này
                .FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.ProductId == productId);

            if (existingItem != null)
            {
                // 2a. Nếu có rồi -> Cộng dồn số lượng
                existingItem.Quantity = (existingItem.Quantity ?? 0) + quantity;
                _context.CartItems.Update(existingItem);
                await _context.SaveChangesAsync();
                return existingItem;
            }
            else
            {
                // 2b. Nếu chưa có -> Tạo mới
                var newItem = new CartItem
                {
                    CartId = cartId,
                    ProductId = productId,
                    Quantity = quantity,
                    CreatedAt = DateTimeHelper.VietnamNow()
                };

                _context.CartItems.Add(newItem);
                await _context.SaveChangesAsync();

                // Load lại Product để có thông tin Price trả về cho DTO
                await _context.Entry(newItem).Reference(i => i.Product).LoadAsync();
                return newItem;
            }
        }

        public async Task UpdateCartItemAsync(CartItem cartItem)
        {
            _context.CartItems.Update(cartItem);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCartItemAsync(CartItem cartItem)
        {
            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
        }

        public async Task ClearCartAsync(int cartId)
        {
            var cartItems = await _context.CartItems
                .Where(ci => ci.CartId == cartId)
                .ToListAsync();

            if (cartItems.Any())
            {
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
            }
        }
    }
}