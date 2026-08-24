namespace Server.Services.AiFoundry;

// interactive-chat-agent-synapse — replaces the old client-side mock for both per-highlight
// Q&A and the general chatbox. existingResponseId is null on the first question (starts a
// fresh exchange) and the prior call's response id on every follow-up, so the agent sees the
// real conversation instead of a fresh, context-less prompt each time.
public class NoteChatAiService(FoundryAgentClient client, IConfiguration configuration, TokenUsageAccumulator tokenUsage)
{
    // The UI shows exactly one question -> one answer per turn — there's no in-place way for
    // the agent to ask a sub-question and get it answered before its response is shown, so
    // asking one just reads as a broken/unhelpful answer. Applied on every turn (not just the
    // first), since follow-ups can trigger this just as easily as an opening question.
    private const string NoClarifyingQuestionsInstruction =
        "Answer directly using your best interpretation of the question and the available " +
        "context — do not ask a clarifying question back or present multiple options for the " +
        "user to choose between; if the question is genuinely ambiguous, briefly state the " +
        "interpretation you're answering under and proceed.";

    public async Task<(string Answer, string ResponseId)> AskAsync(string? existingResponseId, string? sessionContext, string question)
    {
        var agentName = configuration["AzureAiFoundry:Agents:InteractiveChat"]
            ?? throw new InvalidOperationException("AzureAiFoundry:Agents:InteractiveChat is not configured.");

        var contextPrefix = existingResponseId is null && !string.IsNullOrWhiteSpace(sessionContext)
            ? $"Session context (active document):\n{sessionContext}\n\n"
            : "";
        var text = $"{NoClarifyingQuestionsInstruction}\n\n{contextPrefix}Question: {question}";

        var (answer, responseId, tokens) = await client.AskAsync(agentName, text, existingResponseId);
        tokenUsage.Add(tokens);
        return (answer, responseId);
    }
}
