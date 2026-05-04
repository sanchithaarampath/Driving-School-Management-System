using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DSMS.API.Helpers;
using DSMS.API.Services;

namespace DSMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatbotController : ControllerBase
{
    private readonly IChatbotService _chatbot;

    public ChatbotController(IChatbotService chatbot) => _chatbot = chatbot;

    /// <summary>POST /api/chatbot/message</summary>
    [HttpPost("message")]
    public async Task<IActionResult> Message([FromBody] ChatbotRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Message))
            return BadRequest(new { reply = "Please type a message." });

        var role     = ClaimsHelper.GetRole(User);
        var branchId = ClaimsHelper.GetBranchId(User);
        var userName = User.Identity?.Name ?? "User";

        var reply = await _chatbot.ProcessMessageAsync(request.Message, role, branchId, userName);

        return Ok(new { reply });
    }
}

public class ChatbotRequest
{
    public string Message { get; set; } = string.Empty;
}
