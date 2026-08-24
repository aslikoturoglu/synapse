using Microsoft.AspNetCore.Mvc.Filters;
using Server.Controllers;
using Server.Services;

namespace Server.Filters;

// Registered globally (Program.cs) so any endpoint that happens to trigger AI calls gets
// metered automatically — no controller needs to know this exists. Runs after the action so
// TokenUsageAccumulator (Scoped, shared with whichever *AiService the action used) has
// already collected everything that request's AskAsync calls produced.
public class TokenUsageActionFilter(TokenUsageAccumulator accumulator, TokenUsageService tokenUsageService) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await next();

        if (accumulator.Total > 0 && context.HttpContext.User.Identity?.IsAuthenticated == true)
            await tokenUsageService.RecordAsync(context.HttpContext.User.GetUserId(), accumulator.Total);
    }
}
