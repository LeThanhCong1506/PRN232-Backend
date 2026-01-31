using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.RequestModels;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.Interfaces;
using MV.InfrastructureLayer.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MV.ApplicationLayer.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IProductRepository _productRepository;

        public CartService(ICartRepository cartRepository, IProductRepository productRepository)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
        }

        public async Task<ApiResponse<CartResponseDto>> GetCartAsync(int userId)
        {
            // 1. Get Cart from DB
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);

            // 2. If cart doesn't exist, create a new empty one
            if (cart == null)
            {
                cart = await _cartRepository.CreateCartAsync(userId);
            }

            // 3. Map Entity to DTO
            var cartDto = new CartResponseDto
            {
                CartId = cart.CartId,
                Items = cart.CartItems.Select(ci => new CartItemDto
                {
                    CartItemId = ci.CartItemId,
                    Quantity = (int)ci.Quantity,
                    Product = new CartProductDto
                    {
                        ProductId = ci.Product.ProductId,
                        Name = ci.Product.Name,
                        Sku = ci.Product.Sku,
                        Price = ci.Product.Price,
                        StockQuantity = ci.Product.StockQuantity ?? 0,
                        PrimaryImage = ci.Product.ProductImages?.FirstOrDefault()?.ImageUrl ?? "/images/no-image.png",
                        InStock = (ci.Product.StockQuantity ?? 0) > 0
                    },
                    // Calculate Item Total
                    ItemTotal = (decimal)(ci.Quantity) * (ci.Product.Price)
                }).ToList()
            };

            // 4. Calculate Summary
            decimal subtotal = cartDto.Items.Sum(i => i.ItemTotal);
            decimal shippingFee = 30000; // Fixed as per your requirement
            decimal discount = 0; // Logic for coupons can be added later

            cartDto.Summary = new CartSummaryDto
            {
                TotalItems = cartDto.Items.Sum(i => i.Quantity),
                Subtotal = subtotal,
                ShippingFee = shippingFee,
                Discount = discount,
                Total = subtotal + shippingFee - discount
            };

            return ApiResponse<CartResponseDto>.SuccessResponse(cartDto);
        }

        public async Task<ApiResponse<object>> AddToCartAsync(int userId, AddToCartRequestDto request)
        {
            // 1. Kiểm tra sản phẩm tồn tại và check Stock
            var product = await _productRepository.GetProductByIdAsync(request.ProductId);
            if (product == null)
            {
                return ApiResponse<object>.ErrorResponse("Product not found");
            }

            int currentStock = product.StockQuantity ?? 0;

            // CHECK 1: Out of Stock (Kho = 0)
            if (currentStock == 0)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Product out of stock",
                    Data = new { availableQuantity = 0 }
                };
            }

            // 2. Lấy hoặc Tạo giỏ hàng (Get or Create Logic)
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (cart == null)
            {
                cart = await _cartRepository.CreateCartAsync(userId);
            }

            // 3. Lấy số lượng hiện tại của sản phẩm trong giỏ hàng (nếu có)
            int existingQuantity = 0;
            var existingCartItem = cart.CartItems?.FirstOrDefault(ci => ci.ProductId == request.ProductId);
            if (existingCartItem != null)
            {
                existingQuantity = existingCartItem.Quantity ?? 0;
            }

            // CHECK 2: Tổng số lượng (existing + new) không được vượt quá stock
            int totalQuantity = existingQuantity + request.Quantity;
            if (totalQuantity > currentStock)
            {
                int canAddMore = currentStock - existingQuantity;
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Requested quantity exceeds available stock",
                    Data = new { availableQuantity = canAddMore > 0 ? canAddMore : 0 }
                };
            }

            // 4. Thêm vào DB (Repository sẽ tự xử lý cộng dồn)
            var cartItem = await _cartRepository.AddOrUpdateItemAsync(cart.CartId, request.ProductId, request.Quantity);

            // 4. Map kết quả trả về (Success 201)
            var responseData = new CartItemResponseDto
            {
                CartItemId = cartItem.CartItemId,
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity ?? 0,
                ItemTotal = (cartItem.Quantity ?? 0) * (cartItem.Product.Price)
            };

            return new ApiResponse<object>
            {
                Success = true,
                Message = "Product added to cart",
                Data = responseData
            };
        }

        public async Task<ApiResponse<object>> UpdateCartItemQuantityAsync(int userId, int cartItemId, int quantity)
        {
            // 1. Tìm CartItem
            var cartItem = await _cartRepository.GetCartItemByIdAsync(cartItemId);

            // 2. Validate cơ bản
            if (cartItem == null) return ApiResponse<object>.ErrorResponse("Cart item not found");
            if (cartItem.Cart.UserId != userId) return ApiResponse<object>.ErrorResponse("Unauthorized access to this cart item");
            if (quantity < 0) return ApiResponse<object>.ErrorResponse("Quantity cannot be negative");

            // 3. Nếu quantity = 0 -> Xóa cart item
            if (quantity == 0)
            {
                await _cartRepository.DeleteCartItemAsync(cartItem);
                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "Cart item removed",
                    Data = null
                };
            }

            // 4. Check Stock
            int currentStock = cartItem.Product.StockQuantity ?? 0;
            if (quantity > currentStock)
            {
                var errorData = new Dictionary<string, int>
                {
                    { "availableQuantity", currentStock }
                };

                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Requested quantity exceeds available stock",
                    Data = errorData
                };
            }

            // 5. Update
            cartItem.Quantity = quantity;
            await _cartRepository.UpdateCartItemAsync(cartItem);

            // 6. Map kết quả trả về
            var responseData = new CartItemResponseDto
            {
                CartItemId = cartItem.CartItemId,
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity ?? 0,
                ItemTotal = (cartItem.Quantity ?? 0) * (cartItem.Product.Price)
            };

            return new ApiResponse<object>
            {
                Success = true,
                Message = "Cart item updated",
                Data = responseData
            };
        }

        public async Task<ApiResponse<object>> RemoveCartItemAsync(int userId, int cartItemId)
        {
            // 1. Tìm CartItem
            var cartItem = await _cartRepository.GetCartItemByIdAsync(cartItemId);

            // 2. Validate tồn tại
            if (cartItem == null)
            {
                return ApiResponse<object>.ErrorResponse("Cart item not found");
            }

            // 3. Validate quyền sở hữu (Security Check)
            // Chỉ cho phép xóa nếu CartItem này thuộc về User đang đăng nhập
            if (cartItem.Cart.UserId != userId)
            {
                return ApiResponse<object>.ErrorResponse("Unauthorized access to this cart item");
            }

            // 4. Xóa
            await _cartRepository.DeleteCartItemAsync(cartItem);

            // 5. Trả về thông báo thành công
            return new ApiResponse<object>
            {
                Success = true,
                Message = "Cart item removed",
                Data = null // Không cần trả về data gì thêm
            };
        }

        public async Task<ApiResponse<object>> ClearCartAsync(int userId)
        {
            // 1. Lấy giỏ hàng của user
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);

            // 2. Nếu chưa có cart hoặc cart rỗng -> vẫn trả về success
            if (cart == null || cart.CartItems == null || !cart.CartItems.Any())
            {
                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "Cart cleared",
                    Data = null
                };
            }

            // 3. Xóa tất cả cart items (giữ lại cart record)
            await _cartRepository.ClearCartAsync(cart.CartId);

            // 4. Trả về thành công
            return new ApiResponse<object>
            {
                Success = true,
                Message = "Cart cleared",
                Data = null
            };
        }
    }
}