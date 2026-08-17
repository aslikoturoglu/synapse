using System.Text.Json;
using Server.Dtos;

namespace Server.Services.AiFoundry;

// brain-map-agent-synapse runs in two distinct modes at two distinct pipeline points now:
// draft (right after upload — keyword list only, no edges invented yet) and final (once
// topic-synthesizer-agent-synapse's document is complete — enriches, never rebuilds, the
// existing draft with real node/edge relationships grounded primarily in that finished
// document, with the Document RAG knowledge base as secondary supporting depth).
public class NoteMapAiService(FoundryAgentClient client, IConfiguration configuration)
{
    public async Task<GraphDto> GenerateDraftGraphAsync(IReadOnlyList<string> keywords)
    {
        var keywordList = string.Join(", ", keywords);
        var (json, _) = await client.AskAsync(
            RequireAgentName(),
            $"START_DRAFT_BRAINMAP — keywords (merged preliminary-scan + user-typed): {keywordList}");

        return Parse(json);
    }

    public async Task<GraphDto> GenerateFinalGraphAsync(IReadOnlyList<string> draftKeywords, string synthesizedDocument, string ragKnowledgeBase)
    {
        var keywordList = string.Join(", ", draftKeywords);
        var (json, _) = await client.AskAsync(
            RequireAgentName(),
            $"START_FINAL_BRAINMAP — draft_keywords (latest user-edited draft list, already user-confirmed nodes — never silently drop these): {keywordList}\n\n" +
            "Enrich the existing draft brain map using all node/edge relationships derivable from the finalized document below; do not rebuild from scratch.\n\n" +
            $"document_ref — the finalized document (PRIMARY grounding source for relationships):\n{synthesizedDocument}\n\n" +
            $"Document RAG knowledge base (secondary — supporting depth only, never introduce a concept absent from the finalized document above):\n{ragKnowledgeBase}");

        return Parse(json);
    }

    private static GraphDto Parse(string json) =>
        JsonSerializer.Deserialize<GraphDto>(
            JsonExtractor.SelectJsonObject(json, "nodes"),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new GraphDto();

    private string RequireAgentName() =>
        configuration["AzureAiFoundry:Agents:BrainMap"]
            ?? throw new InvalidOperationException("AzureAiFoundry:Agents:BrainMap is not configured.");
}
