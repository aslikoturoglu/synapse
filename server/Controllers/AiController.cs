using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Dtos;
using Server.Services.AiFoundry;

namespace Server.Controllers;

// The note-creation half of the AI pipeline. None of these touch the database — no Post
// exists yet at this point in the wizard, the client folds the resulting Pages/knowledge
// base/final graph into its normal POST /api/posts call afterward. See PostsController for
// the Map (brain-map-agent-synapse regenerate) and Ask AI (interactive-chat-agent-synapse)
// endpoints, which do touch an existing Post.
[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController(NoteCreationAiService aiService, NoteMapAiService mapAiService) : ControllerBase
{
    // Fires document-rag-agent-synapse and orchestrator-chat-agent-synapse's preliminary
    // scan immediately on upload, then brain-map-agent-synapse's draft mode from the scan's
    // keywords — none of it waits on user input, per the Orchestrator's own spec.
    [HttpPost("start")]
    [RequestSizeLimit(100_000_000)]
    public async Task<ActionResult<OrchestratorStartResponse>> Start([FromForm] OrchestratorStartRequest request)
    {
        var fileData = new List<(string Name, byte[] Bytes)>();
        foreach (var file in request.Files)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            fileData.Add((file.FileName, stream.ToArray()));
        }

        var (threadId, scan, knowledgeBase, draftGraph, fileIds) = await aiService.StartAsync(fileData, request.Description ?? "");

        return Ok(new OrchestratorStartResponse
        {
            ThreadId = threadId,
            FileIds = fileIds,
            PreliminaryScan = scan,
            DocumentKnowledgeBase = knowledgeBase,
            DraftGraph = draftGraph,
        });
    }

    // Gated client-side on all three required inputs being ready (RAG done, user intake
    // submitted, final keyword list confirmed) — maps the user's free-text format preference
    // into synthesis_mode via the Orchestrator, then runs topic-synthesizer-agent-synapse.
    [HttpPost("generate-note")]
    public async Task<ActionResult<GenerateNoteResponse>> GenerateNote(GenerateNoteRequest request)
    {
        var (pages, synthesizedMarkdown) = await aiService.GenerateDocumentAsync(
            request.ThreadId, request.DocumentKnowledgeBase, request.FormatPreference,
            request.UserNotes, request.FinalKeywords, request.PreliminaryClassification);

        return Ok(new GenerateNoteResponse { Pages = pages, SynthesizedMarkdown = synthesizedMarkdown });
    }

    // brain-map-agent-synapse's final mode — enriches the existing draft map using the
    // now-finished document as its primary grounding source. Runs once, right after
    // generate-note, as the last step of the creation pipeline.
    [HttpPost("generate-final-brain-map")]
    public async Task<ActionResult<GraphDto>> GenerateFinalBrainMap(GenerateFinalBrainMapRequest request)
    {
        var graph = await mapAiService.GenerateFinalGraphAsync(request.DraftKeywords, request.SynthesizedMarkdown, request.DocumentKnowledgeBase);
        return Ok(graph);
    }
}
