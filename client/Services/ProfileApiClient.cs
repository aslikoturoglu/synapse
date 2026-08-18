using System.Net.Http.Json;

namespace Client.Services;

public class ProfileApiClient(HttpClient http)
{
    // Prefer these whenever the caller already knows the id (e.g. from one of that author's
    // posts) — display name isn't unique, so the name-based lookups below can resolve to the
    // wrong account when two users share a name.
    public async Task<ProfileSummaryDto?> GetSummaryByIdAsync(int userId) =>
        await http.GetFromJsonAsync<ProfileSummaryDto>($"api/profiles/by-id/{userId}");

    public async Task<List<ConnectionDto>> GetFollowersByIdAsync(int userId) =>
        await http.GetFromJsonAsync<List<ConnectionDto>>($"api/profiles/by-id/{userId}/followers") ?? [];

    public async Task<List<ConnectionDto>> GetFollowingByIdAsync(int userId) =>
        await http.GetFromJsonAsync<List<ConnectionDto>>($"api/profiles/by-id/{userId}/following") ?? [];

    public async Task<List<PostDto>> GetRepostsByIdAsync(int userId) =>
        await http.GetFromJsonAsync<List<PostDto>>($"api/profiles/by-id/{userId}/reposts") ?? [];

    public async Task<ProfileSummaryDto?> GetSummaryAsync(string name) =>
        await http.GetFromJsonAsync<ProfileSummaryDto>($"api/profiles/{Uri.EscapeDataString(name)}");

    public async Task<List<ConnectionDto>> GetFollowersAsync(string name) =>
        await http.GetFromJsonAsync<List<ConnectionDto>>($"api/profiles/{Uri.EscapeDataString(name)}/followers") ?? [];

    public async Task<List<ConnectionDto>> GetFollowingAsync(string name) =>
        await http.GetFromJsonAsync<List<ConnectionDto>>($"api/profiles/{Uri.EscapeDataString(name)}/following") ?? [];

    public async Task<List<PostDto>> GetRepostsAsync(string name) =>
        await http.GetFromJsonAsync<List<PostDto>>($"api/profiles/{Uri.EscapeDataString(name)}/reposts") ?? [];
}
