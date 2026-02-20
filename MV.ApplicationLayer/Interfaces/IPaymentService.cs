using MV.DomainLayer.DTOs.Payment.Request;
using MV.DomainLayer.DTOs.Payment.Response;
using MV.DomainLayer.DTOs.ResponseModels;

namespace MV.ApplicationLayer.Interfaces;

public interface IPaymentService
{
    /// <summary>
    /// Xử lý webhook từ SePay khi có giao dịch mới
    /// </summary>
    Task<ApiResponse<object>> ProcessWebhookAsync(SepayWebhookRequest request);

    /// <summary>
    /// Lấy trạng thái thanh toán cho frontend polling
    /// </summary>
    Task<ApiResponse<PaymentStatusResponse>> GetPaymentStatusAsync(int orderId, int userId, bool isAdmin);

    /// <summary>
    /// Admin xác nhận thanh toán thủ công (cho trường hợp webhook lỗi)
    /// </summary>
    Task<ApiResponse<PaymentStatusResponse>> VerifyPaymentManuallyAsync(int orderId, int adminUserId);

    /// <summary>
    /// Xử lý callback khi SePay redirect về sau thanh toán thành công.
    /// Cập nhật Payment status → COMPLETED, Order status → CONFIRMED.
    /// </summary>
    Task<ApiResponse<PaymentStatusResponse>> ProcessSuccessCallbackAsync(string orderInvoiceNumber);

    /// <summary>
    /// Xử lý các payment SEPAY hết hạn - gọi từ background service
    /// </summary>
    Task ExpireOverduePaymentsAsync();
}
