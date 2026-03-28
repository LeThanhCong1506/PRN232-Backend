using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.DTOs.ReturnRequest;

namespace MV.ApplicationLayer.Interfaces;

public interface IReturnRequestService
{
    Task<ApiResponse<ReturnRequestResponse>> CreateReturnRequestAsync(int userId, CreateReturnRequestDto dto);
    Task<ApiResponse<PagedResponse<ReturnRequestResponse>>> GetMyReturnRequestsAsync(int userId, int page, int pageSize);
    Task<ApiResponse<PagedResponse<ReturnRequestResponse>>> GetAllReturnRequestsAsync(int page, int pageSize);
    Task<ApiResponse<ReturnRequestResponse>> GetByIdAsync(int returnRequestId, int userId, bool isAdmin);
    Task<ApiResponse<ReturnRequestResponse>> ProcessReturnRequestAsync(int returnRequestId, int adminUserId, ProcessReturnRequestDto dto);
}
