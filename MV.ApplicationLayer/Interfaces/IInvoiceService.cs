using MV.DomainLayer.DTOs.Invoice.Request;
using MV.DomainLayer.DTOs.Invoice.Response;
using MV.DomainLayer.DTOs.ResponseModels;

namespace MV.ApplicationLayer.Interfaces;

public interface IInvoiceService
{
    Task<ApiResponse<InvoicePreviewResponse>> GenerateInvoicePreviewAsync(int orderId, int currentUserId, bool isAdminOrStaff, GenerateInvoiceRequest request);
}
