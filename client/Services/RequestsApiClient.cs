using System.Net.Http.Json;

namespace Client.Services;

public class RequestsApiClient(HttpClient http)
{
    public async Task<List<UserRequestDto>> GetAllAsync() =>
        await http.GetFromJsonAsync<List<UserRequestDto>>("api/requests") ?? [];

    public async Task<List<UserRequestDto>> GetMineAsync() =>
        await http.GetFromJsonAsync<List<UserRequestDto>>("api/requests/mine") ?? [];

    public async Task<UserRequestDto?> CreateAsync(string subject, string content)
    {
        var response = await http.PostAsJsonAsync("api/requests", new CreateUserRequestRequest { Subject = subject, Content = content });
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<UserRequestDto>() : null;
    }

    public async Task<UserRequestDto?> CreateRoleChangeAsync()
    {
        var response = await http.PostAsync("api/requests/role-change", null);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<UserRequestDto>() : null;
    }

    public async Task<bool> MarkSeenAsync(int id) =>
        (await http.PostAsync($"api/requests/{id}/seen", null)).IsSuccessStatusCode;

    public async Task<bool> RespondAsync(int id, string? reply, string? decision) =>
        (await http.PutAsJsonAsync($"api/requests/{id}/respond", new RespondToRequestRequest { Reply = reply, Decision = decision })).IsSuccessStatusCode;
}
