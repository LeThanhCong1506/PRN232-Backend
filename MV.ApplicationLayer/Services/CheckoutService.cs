using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Checkout.Request;
using MV.DomainLayer.DTOs.Checkout.Response;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.Enums;
using MV.DomainLayer.Interfaces;
using MV.InfrastructureLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

public class CheckoutService : ICheckoutService
{
    private readonly ICartRepository _cartRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly IProductBundleRepository _bundleRepository;

    public CheckoutService(
        ICartRepository cartRepository,
        IUserRepository userRepository,
        ICouponRepository couponRepository,
        IProductBundleRepository bundleRepository)
    {
        _cartRepository = cartRepository;
        _userRepository = userRepository;
        _couponRepository = couponRepository;
        _bundleRepository = bundleRepository;
    }

    public async Task<ApiResponse<ValidateCheckoutResponse>> ValidateCheckoutAsync(int userId, ValidateCheckoutRequest request)
    {
        // 1. Get cart
        var cart = await _cartRepository.GetCartByUserIdAsync(userId);
        
        // 2. Check empty cart
        if (cart == null || cart.CartItems == null || !cart.CartItems.Any())
        {
            return ApiResponse<ValidateCheckoutResponse>.ErrorResponse("Cart is empty");
        }

        // 3. Validate stock for each item and collect errors
        var stockErrors = new List<StockErrorDto>();
        var cartItemDtos = new List<CheckoutCartItemDto>();

        foreach (var item in cart.CartItems)
        {
            var product = item.Product;
            int availableStock = 0;
            bool isAvailable = true;

            // Check stock based on product type
            if (product.ProductType == ProductTypeEnum.KIT)
            {
                // For KIT: Calculate available stock from components
                var components = await _bundleRepository.GetBundleComponentsAsync(product.ProductId);
                
                if (components.Any())
                {
                    int minKits = int.MaxValue;
                    foreach (var component in components)
                    {
                        int componentStock = component.ChildProduct.StockQuantity ?? 0;
                        int requiredQty = component.Quantity ?? 1;
                        int possibleKits = componentStock / requiredQty;
                        
                        if (possibleKits < minKits)
                        {
                            minKits = possibleKits;
                        }
                    }
                    availableStock = minKits;
                }
                else
                {
                    availableStock = 0;
                }
            }
            else
            {
                // For MODULE/COMPONENT: Use product stock directly
                availableStock = product.StockQuantity ?? 0;
            }

            // Check if requested quantity is available
            int requestedQty = item.Quantity ?? 0;
            if (requestedQty > availableStock)
            {
                isAvailable = false;
                stockErrors.Add(new StockErrorDto
                {
                    ProductId = product.ProductId,
                    ProductName = product.Name,
                    RequestedQuantity = requestedQty,
                    AvailableQuantity = availableStock
                });
            }

            // Build cart item DTO
            cartItemDtos.Add(new CheckoutCartItemDto
            {
                CartItemId = item.CartItemId,
                ProductId = product.ProductId,
                ProductName = product.Name,
                Sku = product.Sku,
                Quantity = requestedQty,
                UnitPrice = product.Price,
                ItemTotal = requestedQty * product.Price,
                StockQuantity = availableStock,
                IsAvailable = isAvailable
            });
        }

        // If there are stock errors, return error response with details
        if (stockErrors.Any())
        {
            var errorResponse = new ValidateCheckoutResponse
            {
                IsValid = false,
                CartItems = cartItemDtos,
                StockErrors = stockErrors,
                ShippingInfo = null,
                Coupon = null,
                Summary = null
            };

            return new ApiResponse<ValidateCheckoutResponse>
            {
                Success = false,
                Message = "Some items are out of stock or insufficient quantity",
                Data = errorResponse
            };
        }

        // 4. Get user and validate shipping info
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse<ValidateCheckoutResponse>.ErrorResponse("User not found");
        }

        var missingFields = new List<string>();
        if (string.IsNullOrWhiteSpace(user.Phone))
        {
            missingFields.Add("phone");
        }
        if (string.IsNullOrWhiteSpace(user.Address))
        {
            missingFields.Add("address");
        }

        if (missingFields.Any())
        {
            return ApiResponse<ValidateCheckoutResponse>.ErrorResponse(
                "Please update your shipping information before checkout",
                missingFields
            );
        }

        // 5. Calculate subtotal
        decimal subtotal = cartItemDtos.Sum(item => item.ItemTotal);
        decimal shippingFee = 30000; // Fixed
        decimal discount = 0;
        var couponDto = new CheckoutCouponDto
        {
            IsApplied = false,
            Code = null,
            DiscountAmount = 0
        };

        // 6. Validate coupon if provided
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var coupon = await _couponRepository.GetCouponByCodeAsync(request.CouponCode);
            
            if (coupon != null)
            {
                var now = DateTime.UtcNow;
                bool isValid = true;

                // Check expiration
                if (now < coupon.StartDate || now > coupon.EndDate)
                {
                    isValid = false;
                }

                // Check usage limit
                if (isValid && coupon.UsageLimit.HasValue && coupon.UsedCount.HasValue)
                {
                    if (coupon.UsedCount >= coupon.UsageLimit)
                    {
                        isValid = false;
                    }
                }

                // Check min order value
                if (isValid && coupon.MinOrderValue.HasValue && subtotal < coupon.MinOrderValue.Value)
                {
                    return ApiResponse<ValidateCheckoutResponse>.ErrorResponse("Coupon minimum order value not met");
                }

                if (isValid)
                {
                    discount = coupon.DiscountValue;
                    
                    // Ensure discount doesn't exceed subtotal
                    if (discount > subtotal)
                    {
                        discount = subtotal;
                    }

                    couponDto.IsApplied = true;
                    couponDto.Code = coupon.Code;
                    couponDto.DiscountAmount = discount;
                }
                else
                {
                    return ApiResponse<ValidateCheckoutResponse>.ErrorResponse("Coupon is invalid or expired");
                }
            }
            else
            {
                return ApiResponse<ValidateCheckoutResponse>.ErrorResponse("Coupon is invalid or expired");
            }
        }

        // 7. Build response
        var response = new ValidateCheckoutResponse
        {
            IsValid = true,
            CartItems = cartItemDtos,
            ShippingInfo = new CheckoutShippingInfoDto
            {
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address
            },
            Coupon = couponDto,
            Summary = new CheckoutSummaryDto
            {
                Subtotal = subtotal,
                ShippingFee = shippingFee,
                Discount = discount,
                Total = subtotal + shippingFee - discount
            }
        };

        return ApiResponse<ValidateCheckoutResponse>.SuccessResponse(response);
    }

    public async Task<ApiResponse<ShippingInfoResponse>> GetShippingInfoAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        
        if (user == null)
        {
            return ApiResponse<ShippingInfoResponse>.ErrorResponse("User not found");
        }

        var response = new ShippingInfoResponse
        {
            Username = user.Username,
            Email = user.Email,
            Phone = user.Phone,
            Address = user.Address
        };

        // Add message if info is incomplete
        if (string.IsNullOrWhiteSpace(user.Phone) || string.IsNullOrWhiteSpace(user.Address))
        {
            return ApiResponse<ShippingInfoResponse>.SuccessResponse(response, "Please complete your shipping information");
        }

        return ApiResponse<ShippingInfoResponse>.SuccessResponse(response);
    }

    public async Task<ApiResponse<List<PaymentMethodDto>>> GetPaymentMethodsAsync()
    {
        var paymentMethods = new List<PaymentMethodDto>
        {
            new PaymentMethodDto
            {
                Code = "COD",
                Name = "Cash on Delivery",
                Description = "Thanh toán khi nhận hàng",
                IsActive = true
            },
            new PaymentMethodDto
            {
                Code = "BANK_TRANSFER",
                Name = "Bank Transfer",
                Description = "Chuyển khoản ngân hàng",
                IsActive = true
            }
        };

        return ApiResponse<List<PaymentMethodDto>>.SuccessResponse(paymentMethods);
    }
}
