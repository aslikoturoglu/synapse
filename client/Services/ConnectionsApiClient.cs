using System.Net.Http.Json;

namespace Client.Services;

public class ConnectionsApiClient(HttpClient http)
{
    public async Task<List<ConnectionDto>> GetMineAsync() =>
        await http.GetFromJsonAsync<List<ConnectionDto>>("api/connections") ?? [];

    public async Task<bool?> ToggleFollowAsync(int userId)
    {
        var response = await http.PostAsync($"api/connections/{userId}/toggle", null);
        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<FollowResponse>();
        return result?.Following;
    }
}
