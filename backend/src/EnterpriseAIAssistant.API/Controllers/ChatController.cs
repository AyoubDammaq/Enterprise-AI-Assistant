using EnterpriseAIAssistant.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseAIAssistant.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController(IAIChatService chatService) : ControllerBase
    {
        private readonly IAIChatService _chatService = chatService;

        [HttpPost]
        public async Task<IActionResult> Chat(
            [FromBody] ChatRequest request,
            CancellationToken cancellationToken)
        {
            var response = await _chatService.GetResponseAsync(
                request.Message,
                cancellationToken);

            return Ok(new ChatResponse(response));
        }
    }

    public record ChatRequest(string Message);

    public record ChatResponse(string Message);
}
