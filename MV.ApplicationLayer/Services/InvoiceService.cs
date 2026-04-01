using Microsoft.EntityFrameworkCore;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Invoice.Request;
using MV.DomainLayer.DTOs.Invoice.Response;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.InfrastructureLayer.DBContext;

namespace MV.ApplicationLayer.Services;

public class InvoiceService : IInvoiceService
{
    private readonly StemDbContext _context;

    public InvoiceService(StemDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<InvoicePreviewResponse>> GenerateInvoicePreviewAsync(
        int orderId,
        int currentUserId,
        bool isAdminOrStaff,
        GenerateInvoiceRequest request)
    {
        var validationErrors = ValidateInvoiceRequest(request);
        if (validationErrors.Any())
            return ApiResponse<InvoicePreviewResponse>.ErrorResponse("Invalid invoice payload.", validationErrors);

        var order = await _context.OrderHeaders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            return ApiResponse<InvoicePreviewResponse>.ErrorResponse($"Order with ID {orderId} not found.");

        if (!isAdminOrStaff && order.UserId != currentUserId)
            return ApiResponse<InvoicePreviewResponse>.ErrorResponse("Unauthorized to access this order invoice.");

        var invoiceType = request.InvoiceType.ToUpperInvariant();
        var billingName = invoiceType == "COMPANY" ? request.CompanyName!.Trim() : request.PersonalName!.Trim();

        var response = new InvoicePreviewResponse
        {
            InvoiceNumber = $"INV-{order.OrderNumber}",
            InvoiceType = invoiceType,
            IssuedAt = DateTime.UtcNow,
            TaxCode = request.TaxCode.Trim(),
            BillingName = billingName,
            RepresentativeName = request.RepresentativeName?.Trim(),
            BillingAddress = request.BillingAddress.Trim(),
            Order = new InvoicePreviewResponse.OrderSnapshotInfo
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                CreatedAt = order.CreatedAt,
                CustomerName = order.CustomerName,
                CustomerEmail = order.CustomerEmail,
                CustomerPhone = order.CustomerPhone,
                SubtotalAmount = order.SubtotalAmount,
                ShippingFee = order.ShippingFee ?? 0,
                DiscountAmount = order.DiscountAmount ?? 0,
                TotalAmount = order.TotalAmount
            },
            Items = order.OrderItems.Select(i => new InvoicePreviewResponse.InvoiceItemInfo
            {
                OrderItemId = i.OrderItemId,
                ProductId = i.ProductId,
                ProductName = i.ProductName ?? "Unknown product",
                ProductSku = i.ProductSku,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal
            }).ToList(),
            Payment = order.Payment == null
                ? null
                : new InvoicePreviewResponse.PaymentSnapshotInfo
                {
                    PaymentId = order.Payment.PaymentId,
                    Amount = order.Payment.Amount,
                    PaymentDate = order.Payment.PaymentDate,
                    TransactionId = order.Payment.TransactionId,
                    PaymentReference = order.Payment.PaymentReference,
                    ReceivedAmount = order.Payment.ReceivedAmount
                }
        };

        return ApiResponse<InvoicePreviewResponse>.SuccessResponse(response, "Invoice preview generated successfully.");
    }

    private static List<string> ValidateInvoiceRequest(GenerateInvoiceRequest request)
    {
        var errors = new List<string>();
        var invoiceType = request.InvoiceType.ToUpperInvariant();

        if (!request.TaxCode.All(char.IsDigit))
            errors.Add("Tax code must contain digits only.");

        if (invoiceType == "PERSONAL")
        {
            if (string.IsNullOrWhiteSpace(request.PersonalName))
                errors.Add("PersonalName is required for PERSONAL invoice.");
        }

        if (invoiceType == "COMPANY")
        {
            if (string.IsNullOrWhiteSpace(request.CompanyName))
                errors.Add("CompanyName is required for COMPANY invoice.");

            if (string.IsNullOrWhiteSpace(request.RepresentativeName))
                errors.Add("RepresentativeName is required for COMPANY invoice.");
        }

        return errors;
    }
}
