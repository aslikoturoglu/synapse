using System.Text.Json;
using Server.Dtos;

namespace Server.Services.AiFoundry;

// Orchestrates note creation. orchestrator-chat-agent-synapse is only called for the two
// stages that carry genuinely new information only it can produce: the preliminary scan
// (candidate keywords/language/domain, extracted from the documents) and mapping the user's
// free-text format preference into synthesis_mode. The other pipeline stages it documents
// (START_DOCUMENT_RAG, START_DRAFT_BRAINMAP, START_FINAL_BRAINMAP) have trigger payloads
// made entirely of data this server already has (file ids, the current keyword list) — those
// call document-rag-agent-synapse / brain-map-agent-synapse directly instead of round-tripping
// through the Orchestrator first, same net pipeline behavior for fewer LLM calls. See
// NoteMapAiService for brain-map-agent-synapse and NoteChatAiService for
// interactive-chat-agent-synapse.
public class NoteCreationAiService(FoundryAgentClient client, IConfiguration configuration, NoteMapAiService mapAiService)
{
    private const string NoClarifyingQuestionsInstruction =
        "This is a fully automated pipeline step with no further user interaction possible — " +
        "you will not get a reply. Do not ask clarifying questions or present multiple options " +
        "for the user to pick from. Make your own reasonable judgment calls where the input is " +
        "ambiguous and produce your complete output directly, in the exact format your " +
        "instructions specify.";

    // Uploads the files, then runs the Orchestrator's preliminary scan and document-rag-agent
    // concurrently — both start immediately on upload, per the Orchestrator's own spec ("the
    // moment file(s) are uploaded... do NOT wait for any user input, menu selection, or
    // confirmation"). Once the scan's keywords are back, immediately also runs
    // brain-map-agent's draft mode, so the wizard's BrainMap step always has a real
    // agent-produced draft map waiting for it, not just a placeholder.
    public async Task<(string ThreadId, PreliminaryScanDto Scan, string DocumentKnowledgeBase, GraphDto DraftGraph, List<string> FileIds)> StartAsync(
        IReadOnlyList<(string Name, byte[] Bytes)> files, string description)
    {
        var fileIds = new List<string>();
        foreach (var (name, bytes) in files)
            fileIds.Add(await client.UploadFileAsync(bytes, name));

        var scanTask = RunPreliminaryScanAsync(fileIds, description);
        var ragTask = client.AskAsync(
            RequireAgentName("DocumentRag"),
            $"{NoClarifyingQuestionsInstruction}\n\nAnalyze these uploaded documents in detail.",
            fileIds: fileIds);

        await Task.WhenAll(scanTask, ragTask);

        var (threadId, scan) = await scanTask;
        var (knowledgeBase, _) = await ragTask;
        var draftGraph = await mapAiService.GenerateDraftGraphAsync(scan.SeedKeywords);

        return (threadId, scan, knowledgeBase, draftGraph, fileIds);
    }

    // Gated on all three of the Orchestrator's required inputs (RAG done, user intake, final
    // keyword list) being ready by the time the wizard reaches Processing — the client can't
    // continue past BrainMap without them, so that gate is enforced by the wizard's own step
    // order rather than re-checked here.
    public async Task<(List<NotePageDto> Pages, string SynthesizedMarkdown)> GenerateDocumentAsync(
        string threadId, string documentKnowledgeBase, string formatPreference, string userNotes,
        IReadOnlyList<string> finalKeywords, PreliminaryScanDto preliminaryClassification)
    {
        var keywordList = string.Join(", ", finalKeywords);
        var (directiveText, _) = await client.AskAsync(
            RequireAgentName("Orchestrator"),
            "All three synthesis inputs are ready. Document RAG analysis is complete. " +
            $"User's format preference, in their own words (map this to synthesis_mode yourself — never ask the user about synthesis_mode directly): \"{formatPreference}\". " +
            $"User's notes/focus points: \"{userNotes}\". " +
            $"Final, user-edited Brain Map keyword list: {keywordList}. " +
            "Per your instructions, emit your START_TOPIC_SYNTHESIS trigger now.",
            threadId);

        var directive = ParseTopicSynthesisDirective(directiveText, formatPreference, userNotes, finalKeywords, preliminaryClassification);

        var (synthesized, _) = await client.AskAsync(
            RequireAgentName("TopicSynthesizer"),
            $"{NoClarifyingQuestionsInstruction}\n\n" +
            $"synthesis_mode: {directive.SynthesisMode}\n" +
            $"user_notes: {directive.UserNotes}\n" +
            $"final_keywords: {string.Join(", ", directive.FinalKeywords)}\n" +
            $"preliminary_classification: language={directive.PreliminaryClassification.Language}, domain={directive.PreliminaryClassification.Domain}, estimated_structure={directive.PreliminaryClassification.EstimatedStructure}\n" +
            "constraints: no_internet_research=true, no_bibliography=true\n\n" +
            $"LANGUAGE: write the entire output in a single, consistent language — {directive.PreliminaryClassification.Language} (the source documents' own language), unless user_notes above explicitly asks for a different output language, in which case use that instead. " +
            "This applies to EVERYTHING, including your own template's structural labels (the section headings, \"İlgili Anahtar Kavramlar (Brain Map)\", \"İçerik\", \"Bağlantılar ve İlişkiler\", the table of contents heading, etc.) — translate those labels into the chosen language too, not just the content. Never mix languages within the document.\n\n" +
            $"Document RAG knowledge base:\n{documentKnowledgeBase}");

        return (MarkdownPager.SplitIntoPages(synthesized), synthesized);
    }

    private async Task<(string ThreadId, PreliminaryScanDto Scan)> RunPreliminaryScanAsync(IReadOnlyList<string> fileIds, string description)
    {
        var (scanText, responseId) = await client.AskAsync(
            RequireAgentName("Orchestrator"),
            $"{NoClarifyingQuestionsInstruction}\n\nFiles have just been uploaded.{DescribeSuffix(description)} " +
            "Per your instructions, perform your preliminary scan now and report it as your preliminary_scan JSON (seed_keywords, language, domain, estimated_structure).",
            fileIds: fileIds);

        return (responseId, ParsePreliminaryScan(scanText));
    }

    // The wizard no longer has a dedicated description step — description stays an optional
    // parameter (kept for API flexibility) rather than a required one.
    private static string DescribeSuffix(string description) =>
        string.IsNullOrWhiteSpace(description) ? "" : $" User's description of what they're for: \"{description}\".";

    private static PreliminaryScanDto ParsePreliminaryScan(string text)
    {
        using var doc = JsonDocument.Parse(JsonExtractor.SelectJsonObject(text, "preliminary_scan"));
        var root = doc.RootElement;
        var scan = root.TryGetProperty("preliminary_scan", out var nested) ? nested : root;

        return new PreliminaryScanDto
        {
            SeedKeywords = ReadStringArray(scan, "seed_keywords"),
            Language = ReadString(scan, "language"),
            Domain = ReadString(scan, "domain"),
            EstimatedStructure = ReadString(scan, "estimated_structure"),
        };
    }

    private sealed class TopicSynthesisDirective
    {
        public string SynthesisMode { get; set; } = "";
        public string UserNotes { get; set; } = "";
        public List<string> FinalKeywords { get; set; } = [];
        public PreliminaryScanDto PreliminaryClassification { get; set; } = new();
    }

    // Falls back to whatever the server already sent for any field the agent's JSON is
    // missing — the Orchestrator's own synthesis_mode mapping is the one field genuinely
    // worth waiting on; everything else here the server already knew before asking.
    private static TopicSynthesisDirective ParseTopicSynthesisDirective(
        string text, string fallbackFormatPreference, string fallbackUserNotes,
        IReadOnlyList<string> fallbackKeywords, PreliminaryScanDto fallbackClassification)
    {
        using var doc = JsonDocument.Parse(JsonExtractor.SelectJsonObject(text, "synthesis_mode"));
        var root = doc.RootElement;

        var synthesisMode = ReadString(root, "synthesis_mode");
        var finalKeywords = ReadStringArray(root, "final_keywords");
        var userNotes = ReadString(root, "user_notes");

        return new TopicSynthesisDirective
        {
            SynthesisMode = synthesisMode.Length > 0 ? synthesisMode : fallbackFormatPreference,
            UserNotes = userNotes.Length > 0 ? userNotes : fallbackUserNotes,
            FinalKeywords = finalKeywords.Count > 0 ? finalKeywords : [.. fallbackKeywords],
            PreliminaryClassification = ParseClassification(root, fallbackClassification),
        };
    }

    private static PreliminaryScanDto ParseClassification(JsonElement root, PreliminaryScanDto fallback)
    {
        if (!root.TryGetProperty("preliminary_classification", out var el))
            return fallback;

        var language = ReadString(el, "language");
        var domain = ReadString(el, "domain");
        var estimatedStructure = ReadString(el, "estimated_structure");

        return new PreliminaryScanDto
        {
            SeedKeywords = fallback.SeedKeywords,
            Language = language.Length > 0 ? language : fallback.Language,
            Domain = domain.Length > 0 ? domain : fallback.Domain,
            EstimatedStructure = estimatedStructure.Length > 0 ? estimatedStructure : fallback.EstimatedStructure,
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
