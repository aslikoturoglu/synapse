using System.Text.Json;
using Server.Dtos;

namespace Server.Services.AiFoundry;

// Orchestrates the note-creation half of the pipeline: orchestrator-chat-agent-synapse's
// scan+menu turn, then document-rag-agent-synapse + topic-synthesizer-agent-synapse to turn
// the user's edited Brain Map into real generated pages. See NoteMapAiService for
// brain-map-agent-synapse (only used later, from the Map view) and NoteChatAiService for
// interactive-chat-agent-synapse (Ask AI).
public class NoteCreationAiService(FoundryAgentClient client, IConfiguration configuration)
{
    // Every call in this pipeline is one-shot — there's no follow-up turn where a clarifying
    // question could actually get answered, so an agent asking one just produces a broken
    // note (its question ends up embedded in the note as if it were content). Every message
    // to a generation agent says so explicitly.
    private const string NoClarifyingQuestionsInstruction =
        "This is a fully automated pipeline step with no further user interaction possible — " +
        "you will not get a reply. Do not ask clarifying questions or present multiple options " +
        "for the user to pick from. Make your own reasonable judgment calls where the input is " +
        "ambiguous and produce your complete output directly, in the exact format your " +
        "instructions specify.";


    public async Task<(string ResponseId, string Summary, List<string> FileIds)> StartOrchestratorAsync(
        IReadOnlyList<(string Name, byte[] Bytes)> files, string description)
    {
        var fileIds = new List<string>();
        foreach (var (name, bytes) in files)
            fileIds.Add(await client.UploadFileAsync(bytes, name));

        var (summary, responseId) = await client.AskAsync(
            RequireAgentName("Orchestrator"),
            $"Here are the uploaded documents. The user's description of what they're for: \"{description}\". " +
            "Scan them and present your summary and menu.",
            fileIds: fileIds);

        return (responseId, summary, fileIds);
    }

    // The orchestrator's directive is the end of that conversation — nothing downstream
    // continues it, so only the parsed directive is returned, not a new response id.
    public async Task<NoteAiDirectiveDto> SelectOrchestratorMenuAsync(string previousResponseId, string selection)
    {
        var (directiveText, _) = await client.AskAsync(RequireAgentName("Orchestrator"), selection, previousResponseId);
        return ParseDirective(directiveText);
    }

    public async Task<(List<NotePageDto> Pages, string DocumentKnowledgeBase)> GenerateNoteAsync(
        IReadOnlyList<string> fileIds, NoteAiDirectiveDto directive, IReadOnlyList<BrainMapKeywordDto> keywords)
    {
        // document-rag-agent-synapse: reads the files themselves plus the orchestrator's
        // directive, on its own fresh exchange (it's a document-processing pass, not a
        // continuation of the orchestrator's chat).
        var (knowledgeBase, _) = await client.AskAsync(
            RequireAgentName("DocumentRag"),
            $"{NoClarifyingQuestionsInstruction}\n\nAnalyze these documents in detail. Orchestrator directive JSON:\n" +
            JsonSerializer.Serialize(directive),
            fileIds: fileIds);

        // topic-synthesizer-agent-synapse: per its own spec, only the active (possibly
        // user-edited) Brain Map plus document-rag's output — not the raw files again.
        var keywordList = string.Join(", ", keywords.Select(k => k.Text));
        var (synthesized, _) = await client.AskAsync(
            RequireAgentName("TopicSynthesizer"),
            $"{NoClarifyingQuestionsInstruction}\n\nActive Brain Map keywords: {keywordList}\n\n" +
            $"Document RAG knowledge base:\n{knowledgeBase}");

        return (MarkdownPager.SplitIntoPages(synthesized), knowledgeBase);
    }

    private static NoteAiDirectiveDto ParseDirective(string text)
    {
        using var doc = JsonDocument.Parse(JsonExtractor.ExtractJson(text));
        var root = doc.RootElement;

        return new NoteAiDirectiveDto
        {
            SeedKeywords = ReadStringArray(root, "seed_keywords"),
            FocusAreas = ReadStringArray(root, "focus_areas"),
            SynthesisMode = ReadString(root, "synthesis_mode"),
            UserCustomInstructions = ReadString(root, "user_custom_instructions"),
        };
    }

    private static string ReadString(JsonElement root, string property) =>
        root.TryGetProperty(property, out var value) ? value.GetString() ?? "" : "";

    private static List<string> ReadStringArray(JsonElement root, string property) =>
        root.TryGetProperty(property, out var array) && array.ValueKind == JsonValueKind.Array
            ? array.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToList()
            : [];

    private string RequireAgentName(string key) =>
        configuration[$"AzureAiFoundry:Agents:{key}"]
            ?? throw new InvalidOperationException($"AzureAiFoundry:Agents:{key} is not configured.");
}
