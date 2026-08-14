using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Dtos;
using Server.Services.AiFoundry;

namespace Server.Controllers;

// The note-creation half of the AI pipeline (orchestrator-chat-agent-synapse, then
// document-rag-agent-synapse + topic-synthesizer-agent-synapse via GenerateNote). None of
// these touch the database — no Post exists yet at this point in the wizard, the client folds
// the resulting Pages/DocumentKnowledgeBase into its normal POST /api/posts call afterward.
// See PostsController for the Map (brain-map-agent-synapse) and Ask AI
// (interactive-chat-agent-synapse) endpoints, which do touch an existing Post.
[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController(NoteCreationAiService aiService) : ControllerBase
{
    [HttpPost("orchestrator/start")]
    [RequestSizeLimit(100_000_000)]
    public async Task<ActionResult<OrchestratorStartResponse>> StartOrchestrator([FromForm] OrchestratorStartRequest request)
    {
        var fileData = new List<(string Name, byte[] Bytes)>();
        foreach (var file in request.Files)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            fileData.Add((file.FileName, stream.ToArray()));
        }

        var (threadId, summary, fileIds) = await aiService.StartOrchestratorAsync(fileData, request.Description);
        return Ok(new OrchestratorStartResponse { ThreadId = threadId, Summary = summary, FileIds = fileIds });
    }

    [HttpPost("orchestrator/select")]
    public async Task<ActionResult<NoteAiDirectiveDto>> SelectOrchestratorMenu(OrchestratorSelectRequest request)
    {
        var directive = await aiService.SelectOrchestratorMenuAsync(request.ThreadId, request.Selection);
        return Ok(directive);
    }

    [HttpPost("generate-note")]
    public async Task<ActionResult<GenerateNoteResponse>> GenerateNote(GenerateNoteRequest request)
    {
        var (pages, knowledgeBase) = await aiService.GenerateNoteAsync(request.FileIds, request.Directive, request.Keywords);
        return Ok(new GenerateNoteResponse { Pages = pages, DocumentKnowledgeBase = knowledgeBase });
    }
}
