using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Chatbot;
using MV.DomainLayer.DTOs.ResponseModels;
using Swashbuckle.AspNetCore.Annotations;

namespace MV.PresentationLayer.Controllers;

/// <summary>
/// AI Chatbot - Smart assistant for STEM Store
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
    /// Send a question to the AI chatbot
    /// </summary>
    /// <param name="request">Question from user</param>
    /// <returns>Answer + source (faq/ai/fallback/error)</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/chatbot/ask
    ///     {
    ///       "question": "How much does Arduino Uno cost?"
    ///     }
    ///     
    /// Response:
    /// - answer: Answer content
    /// - source: "faq" (keyword match), "ai" (Groq AI), "fallback" (AI not configured), "error"
    /// - timestamp: Response timestamp
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
