using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.DTOs.Invoice.Request;
using MV.DomainLayer.DTOs.Invoice.Response;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.InfrastructureLayer.DBContext;
using System.Security.Claims;

namespace MV.PresentationLayer.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoiceController : ControllerBase
{
    private readonly StemDbContext _context;

    public InvoiceController(StemDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Generate invoice preview from existing order data (runtime only, no DB persistence)
    /// </summary>
    [HttpPost("orders/{orderId}/preview")]
    public async Task<IActionResult> GenerateInvoicePreview(int orderId, [FromBody] GenerateInvoiceRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid Token"));

        var isAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");

        var order = await _context.OrderHeaders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            return NotFound(ApiResponse<object>.ErrorResponse($"Order with ID {orderId} not found."));

        if (!isAdmin && order.UserId != currentUserId.Value)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.ErrorResponse("Unauthorized to access this order invoice."));

        var validationErrors = ValidateInvoiceRequest(request);
        if (validationErrors.Any())
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid invoice payload.", validationErrors));

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

        return Ok(ApiResponse<InvoicePreviewResponse>.SuccessResponse(response, "Invoice preview generated successfully."));
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

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return null;
        return userId;
    }
}
