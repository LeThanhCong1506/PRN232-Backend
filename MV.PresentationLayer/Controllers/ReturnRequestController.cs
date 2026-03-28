using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.ReturnRequest;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace MV.PresentationLayer.Controllers;

[ApiController]
[Route("api/return-request")]
[Authorize]
public class ReturnRequestController : ControllerBase
{
    private readonly IReturnRequestService _service;

    public ReturnRequestController(IReturnRequestService service)
    {
        _service = service;
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create a return/exchange request for a delivered order")]
    public async Task<IActionResult> Create([FromBody] CreateReturnRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.CreateReturnRequestAsync(userId.Value, dto);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("my")]
    [SwaggerOperation(Summary = "Get my return/exchange requests")]
    public async Task<IActionResult> GetMy([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.GetMyReturnRequestsAsync(userId.Value, page, pageSize);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "[Admin] Get all return/exchange requests")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetAllReturnRequestsAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "Get return request detail")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var isAdmin = User.IsInRole("Admin");
        var result = await _service.GetByIdAsync(id, userId.Value, isAdmin);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpPut("{id}/process")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "[Admin] Approve, reject, or complete a return request")]
    public async Task<IActionResult> Process(int id, [FromBody] ProcessReturnRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var adminId = GetCurrentUserId();
        if (adminId == null) return Unauthorized();

        var result = await _service.ProcessReturnRequestAsync(id, adminId.Value, dto);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (claim == null || !int.TryParse(claim.Value, out var id)) return null;
        return id;
    }
}
