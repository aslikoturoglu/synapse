using System.Text.Json;
using Server.Dtos;

namespace Server.Services.AiFoundry;

// brain-map-agent-synapse — only called from the Map view (not part of note creation), fed
// the post's already-persisted document-rag knowledge base plus its current keywords. Its
// strict JSON schema ({nodes, edges}) is parsed straight into GraphDto.
public class NoteMapAiService(FoundryAgentClient client, IConfiguration configuration)
{
    public async Task<GraphDto> GenerateGraphAsync(string documentKnowledgeBase, IReadOnlyList<BrainMapKeywordDto> keywords)
    {
        var agentName = configuration["AzureAiFoundry:Agents:BrainMap"]
            ?? throw new InvalidOperationException("AzureAiFoundry:Agents:BrainMap is not configured.");

        var seedKeywords = string.Join(", ", keywords.Select(k => k.Text));
        var (json, _) = await client.AskAsync(
            agentName,
            $"Orchestrator seed_keywords/focus_areas (current Brain Map): {seedKeywords}\n\n" +
            $"Document RAG knowledge base:\n{documentKnowledgeBase}");

        return JsonSerializer.Deserialize<GraphDto>(
            JsonExtractor.ExtractJson(json),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new GraphDto();
    }
}
