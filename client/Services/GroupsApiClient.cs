using System.Net.Http.Json;

namespace Client.Services;

public class GroupsApiClient(HttpClient http)
{
    public async Task<List<GroupDto>> GetMineAsync() =>
        await http.GetFromJsonAsync<List<GroupDto>>("api/groups/mine") ?? [];

    public async Task<(GroupDto? Group, string? Error)> CreateAsync(string name)
    {
        var response = await http.PostAsJsonAsync("api/groups", new CreateGroupRequest { Name = name });
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<GroupDto>(), null);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        return (null, error?.Error);
    }
}
