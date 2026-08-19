using System.Net.Http.Json;

namespace Client.Services;

// Axon's "ask a question" endpoint — interactive-chat-agent-synapse, not scoped to any note.
public class AssistantApiClient(HttpClient http)
{
    public async Task<AskAiResponse?> AskAsync(string question, string? threadId)
    {
        var response = await http.PostAsJsonAsync("api/assistant/ask", new AskAiRequest { Question = question, ThreadId = threadId });
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<AskAiResponse>() : null;
    }
}
