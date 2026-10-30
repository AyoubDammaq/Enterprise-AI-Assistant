using System.IdentityModel.Tokens.Jwt;
using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseAIAssistant.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController(IAIChatService aiChatService, IRagService ragService) : ControllerBase
    {
        private readonly IAIChatService _aiChatService = aiChatService;
        private readonly IRagService _ragService = ragService;

        [HttpPost]
        public async Task<IActionResult> Chat(
            [FromBody] ChatRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message cannot be empty.");
            }

            // L'identité provient du JWT validé, jamais du corps HTTP.
            var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId) ||
                userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var conversationId = string.IsNullOrWhiteSpace(request.ConversationId)
                ? Guid.NewGuid().ToString()
                : request.ConversationId;

            try
            {
                var response = await _aiChatService.GenerateResponseAsync(
                    request.Message,
                    conversationId,
                    userId,
                    cancellationToken);

                return Ok(new ChatResponse(response, conversationId));
            }
            catch (UnauthorizedAccessException)
            {
                // Ne révèle pas si une conversation appartient à un autre utilisateur.
                return NotFound("Conversation not found.");
            }
        }

        [HttpPost("rag")]
        public async Task<IActionResult> ChatWithRag(
            [FromBody] RagRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest("Question cannot be empty.");
            }

            // L'identité provient du JWT validé.
            var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId) ||
                userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var conversationId = string.IsNullOrWhiteSpace(request.ConversationId)
                ? Guid.NewGuid().ToString()
                : request.ConversationId;

            try
            {
                var response = await _ragService.AskAsync(
                    request.Question,
                    conversationId,
                    userId,
                    cancellationToken);

                return Ok(new
                {
                    response.Answer,
                    response.Sources,
                    ConversationId = conversationId
                });
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound("Conversation not found.");
            }
        }
    }
}
