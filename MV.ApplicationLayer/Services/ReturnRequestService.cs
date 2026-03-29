using Microsoft.EntityFrameworkCore;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.DTOs.ReturnRequest;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Helpers;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

public class ReturnRequestService : IReturnRequestService
{
    private readonly IReturnRequestRepository _repository;
    private readonly StemDbContext _context;
    private readonly INotificationService _notificationService;

    public ReturnRequestService(IReturnRequestRepository repository, StemDbContext context, INotificationService notificationService)
    {
        _repository = repository;
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<ApiResponse<ReturnRequestResponse>> CreateReturnRequestAsync(int userId, CreateReturnRequestDto dto)
    {
        // Validate order belongs to user and is DELIVERED
        var order = await _context.OrderHeaders
            .FirstOrDefaultAsync(o => o.OrderId == dto.OrderId && o.UserId == userId);

        if (order == null)
            return ApiResponse<ReturnRequestResponse>.ErrorResponse("Order not found.");

        // Order status is stored as PostgreSQL enum, query via raw SQL
        var orderStatus = await _context.Database
            .SqlQueryRaw<string>("SELECT status::text AS \"Value\" FROM order_header WHERE order_id = {0}", dto.OrderId)
            .FirstOrDefaultAsync();

        if (orderStatus != "DELIVERED")
            return ApiResponse<ReturnRequestResponse>.ErrorResponse("Return requests can only be made for delivered orders.");

        if (await _repository.HasActiveRequestForOrderAsync(dto.OrderId))
            return ApiResponse<ReturnRequestResponse>.ErrorResponse("An active return/exchange request already exists for this order.");

        var returnRequest = new ReturnRequest
        {
            OrderId = dto.OrderId,
            UserId = userId,
            Type = dto.Type,
            Reason = dto.Reason,
            Status = "SUBMITTED"
        };

        var created = await _repository.CreateAsync(returnRequest);
        var detail = await _repository.GetByIdAsync(created.ReturnRequestId);

        // Notify Admin about new return request
        try
        {
            var customerName = order.CustomerName ?? order.User?.FullName ?? "Unknown";
            var orderNumber = order.OrderNumber ?? "Unknown";
            await _notificationService.SendAdminNewReturnRequestAsync(created.ReturnRequestId, orderNumber, customerName);
        }
        catch { }

        return ApiResponse<ReturnRequestResponse>.SuccessResponse(MapToResponse(detail!), "Return request submitted successfully.");
    }

    public async Task<ApiResponse<PagedResponse<ReturnRequestResponse>>> GetMyReturnRequestsAsync(int userId, int page, int pageSize)
    {
        var (items, total) = await _repository.GetByUserIdPagedAsync(userId, page, pageSize);
        var dtos = items.Select(MapToResponse).ToList();
        return ApiResponse<PagedResponse<ReturnRequestResponse>>.SuccessResponse(
            new PagedResponse<ReturnRequestResponse>(dtos, page, pageSize, total));
    }

    public async Task<ApiResponse<PagedResponse<ReturnRequestResponse>>> GetAllReturnRequestsAsync(int page, int pageSize)
    {
        var (items, total) = await _repository.GetAllPagedAsync(page, pageSize);
        var dtos = items.Select(MapToResponse).ToList();
        return ApiResponse<PagedResponse<ReturnRequestResponse>>.SuccessResponse(
            new PagedResponse<ReturnRequestResponse>(dtos, page, pageSize, total));
    }

    public async Task<ApiResponse<ReturnRequestResponse>> GetByIdAsync(int returnRequestId, int userId, bool isAdmin)
    {
        var request = await _repository.GetByIdAsync(returnRequestId);
        if (request == null)
            return ApiResponse<ReturnRequestResponse>.ErrorResponse("Return request not found.");

        if (!isAdmin && request.UserId != userId)
            return ApiResponse<ReturnRequestResponse>.ErrorResponse("Access denied.");

        return ApiResponse<ReturnRequestResponse>.SuccessResponse(MapToResponse(request));
    }

    public async Task<ApiResponse<ReturnRequestResponse>> ProcessReturnRequestAsync(int returnRequestId, int adminUserId, ProcessReturnRequestDto dto)
    {
        var request = await _repository.GetByIdAsync(returnRequestId);
        if (request == null)
            return ApiResponse<ReturnRequestResponse>.ErrorResponse("Return request not found.");

        // Validate status transition
        var validTransitions = new Dictionary<string, List<string>>
        {
            { "SUBMITTED", new List<string> { "APPROVED", "REJECTED" } },
            { "APPROVED", new List<string> { "COMPLETED" } }
        };

        if (!validTransitions.TryGetValue(request.Status, out var allowed) || !allowed.Contains(dto.Status))
            return ApiResponse<ReturnRequestResponse>.ErrorResponse($"Cannot transition from {request.Status} to {dto.Status}.");

        request.Status = dto.Status;
        request.AdminNote = dto.AdminNote;
        request.ProcessedBy = adminUserId;
        request.ProcessedAt = DateTimeHelper.VietnamNow();

        await _repository.UpdateAsync(request);

        return ApiResponse<ReturnRequestResponse>.SuccessResponse(MapToResponse(request), $"Return request {dto.Status.ToLower()} successfully.");
    }

    private static ReturnRequestResponse MapToResponse(ReturnRequest r) => new ReturnRequestResponse
    {
        ReturnRequestId = r.ReturnRequestId,
        OrderId = r.OrderId,
        OrderNumber = r.Order?.OrderNumber ?? "",
        UserId = r.UserId,
        UserName = r.User?.FullName ?? r.User?.Username ?? "",
        Type = r.Type,
        Reason = r.Reason,
        Status = r.Status,
        AdminNote = r.AdminNote,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        ProcessedAt = r.ProcessedAt,
        ProcessedByName = r.ProcessedByNavigation?.FullName ?? r.ProcessedByNavigation?.Username
    };
}
