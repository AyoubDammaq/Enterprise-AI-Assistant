using EnterpriseAIAssistant.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using EnterpriseAIAssistant.Application.DTOs;

namespace EnterpriseAIAssistant.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController(IAIChatService aiChatService) : ControllerBase
    {
        private readonly IAIChatService _aiChatService = aiChatService;


        [HttpPost]
        public async Task<IActionResult> Chat(
            [FromBody] ChatRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message cannot be empty.");
            }

            if (request.UserId is null ||
                request.UserId == Guid.Empty)
            {
                return BadRequest("A valid UserId is required.");
            }

            var conversationId = string.IsNullOrWhiteSpace(request.ConversationId)
                ? Guid.NewGuid().ToString()
                : request.ConversationId;

            try
            {
                var response = await _aiChatService.GenerateResponseAsync(
                    request.Message,
                    conversationId,
                    request.UserId,
                    cancellationToken);

                return Ok(new ChatResponse(response, conversationId));
            }
            catch (UnauthorizedAccessException)
            {
                // Ne révèle pas si l'identifiant existe pour un autre utilisateur.
                return NotFound("Conversation not found.");
            }
        }
    }
}
