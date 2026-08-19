using EnterpriseAIAssistant.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using EnterpriseAIAssistant.Application.DTOs;

namespace EnterpriseAIAssistant.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController(IAIChatService iaChatService) : ControllerBase
    {
        private readonly IAIChatService _aiChatService = iaChatService;

        [HttpPost]
        public async Task<IActionResult> Chat(
            [FromBody] ChatRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message cannot be empty.");
            }

            var response = await _aiChatService.GenerateResponseAsync(
                request.Message,
                cancellationToken);

            return Ok(new ChatResponse(response));
        }
    }
}
