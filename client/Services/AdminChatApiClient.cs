using System.Net.Http.Json;

namespace Client.Services;

public class AdminChatApiClient(HttpClient http)
{
    public async Task<AdminChatResponse?> AskAsync(string? threadId, string question)
    {
        var response = await http.PostAsJsonAsync("api/admin-chat", new AdminChatRequest { ThreadId = threadId, Question = question });
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<AdminChatResponse>() : null;
    }
}
