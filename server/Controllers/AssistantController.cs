using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Dtos;
using Server.Services.AiFoundry;

namespace Server.Controllers;

// Axon (AssistantWidget.razor) — the "ask a question about the app" function available to
// every signed-in user, any role. Reuses NoteChatAiService as-is: interactive-chat-agent-synapse
// already doubles as "the general chatbox" per its own doc comment, so this just points it at
// a fixed description of Synapse itself instead of a specific note's content.
[ApiController]
[Route("api/assistant")]
[Authorize]
public class AssistantController(NoteChatAiService chatAiService) : ControllerBase
{
    private const string AppContext =
        "You are Axon, the onboarding/help assistant built into Synapse, a note-taking and " +
        "knowledge-sharing app. Answer questions about how to use Synapse itself (not general " +
        "knowledge questions). Key features: creating a note (Create New Note — upload " +
        "documents, AI generates a structured note with a Brain Map of key concepts, a Process " +
        "view for Q&A over highlighted passages, and a Map); sharing a note to the public Home " +
        "feed (with per-note control over whether Brain Map/Process are visible to others); " +
        "All Notes and Favorites for managing your own notes and Groups for organizing them; " +
        "Connections for following other users; My Requests for asking the admin/moderator " +
        "team a question or requesting a role change, tracked as Pending/Approved/Rejected; " +
        "user roles are User, Moderator (can moderate other accounts and handle requests, in " +
        "addition to normal use), and Admin. Keep answers short and concrete.";

    [HttpPost("ask")]
    public async Task<ActionResult<AskAiResponse>> Ask(AskAiRequest request)
    {
        var (answer, responseId) = await chatAiService.AskAsync(request.ThreadId, AppContext, request.Question);
        return Ok(new AskAiResponse { Answer = answer, ThreadId = responseId });
    }
}
