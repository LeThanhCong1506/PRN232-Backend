using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Chatbot;
using MV.DomainLayer.DTOs.ResponseModels;
using Swashbuckle.AspNetCore.Annotations;

namespace MV.PresentationLayer.Controllers;

/// <summary>
/// AI Chatbot - Trợ lý thông minh cho STEM Store
/// </summary>
[ApiController]
[Route("api/chatbot")]
public class ChatbotController : ControllerBase
{
    private readonly IChatbotService _chatbotService;

    public ChatbotController(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    /// <summary>
    /// Gửi câu hỏi cho chatbot AI
    /// </summary>
    /// <param name="request">Câu hỏi từ user</param>
    /// <returns>Câu trả lời + nguồn (faq/ai/fallback/error)</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/chatbot/ask
    ///     {
    ///       "question": "Arduino Uno giá bao nhiêu?"
    ///     }
    ///     
    /// Response:
    /// - answer: Nội dung câu trả lời
    /// - source: "faq" (keyword match), "ai" (Groq AI), "fallback" (AI chưa cấu hình), "error"
    /// - timestamp: Thời gian response
    /// </remarks>
    [HttpPost("ask")]
    [SwaggerOperation(Summary = "Ask chatbot a question")]
    public async Task<IActionResult> Ask([FromBody] ChatbotRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(ApiResponse<ChatbotResponse>.ErrorResponse("Question is required."));
        }

        var response = await _chatbotService.AskAsync(request.Question);
        return Ok(ApiResponse<ChatbotResponse>.SuccessResponse(response));
    }
}
