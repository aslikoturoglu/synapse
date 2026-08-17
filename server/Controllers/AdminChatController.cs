using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Dtos;
using Server.Services;
using Server.Services.AiFoundry;

namespace Server.Controllers;

[ApiController]
[Route("api/admin-chat")]
[Authorize(Roles = "Admin")]
public class AdminChatController(AdminChatAiService chatService, UserService userService, DashboardService dashboardService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AdminChatResponse>> Ask(AdminChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { error = "Question is required." });

        var users = await userService.GetAllAdminAsync();
        var (start, end) = DashboardService.ResolveRange("total", null, null);
        var stats = await dashboardService.GetStatsAsync(start, end);

        var (answer, responseId) = await chatService.AskAsync(request.ThreadId, users, stats, request.Question.Trim());
        return Ok(new AdminChatResponse { Answer = answer, ThreadId = responseId });
    }
}
