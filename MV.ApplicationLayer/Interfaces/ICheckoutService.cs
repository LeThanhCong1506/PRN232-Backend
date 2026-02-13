using MV.DomainLayer.DTOs.Checkout.Request;
using MV.DomainLayer.DTOs.Checkout.Response;
using MV.DomainLayer.DTOs.ResponseModels;

namespace MV.ApplicationLayer.Interfaces;

public interface ICheckoutService
{
    Task<ApiResponse<ValidateCheckoutResponse>> ValidateCheckoutAsync(int userId, ValidateCheckoutRequest request);
    Task<ApiResponse<ShippingInfoResponse>> GetShippingInfoAsync(int userId);
    Task<ApiResponse<List<PaymentMethodDto>>> GetPaymentMethodsAsync();
}
