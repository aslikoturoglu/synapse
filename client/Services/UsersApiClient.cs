using System.Net.Http.Json;

namespace Client.Services;

public class UsersApiClient(HttpClient http)
{
    public async Task<UserDto?> GetByIdAsync(int id) =>
        await http.GetFromJsonAsync<UserDto>($"api/users/{id}");

    public async Task<List<AdminUserDto>> GetAllAsync() =>
        await http.GetFromJsonAsync<List<AdminUserDto>>("api/users") ?? [];

    public async Task<bool> DeactivateAsync(int id, string reason) =>
        (await http.PostAsJsonAsync($"api/users/{id}/deactivate", new DeactivateUserRequest { Reason = reason })).IsSuccessStatusCode;

    public async Task<bool> ReactivateAsync(int id) =>
        (await http.PostAsync($"api/users/{id}/reactivate", null)).IsSuccessStatusCode;

    public async Task<string?> ResetPasswordAsync(int id)
    {
        var response = await http.PostAsync($"api/users/{id}/reset-password", null);
        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<ResetPasswordResponse>();
        return result?.NewPassword;
    }

    public async Task<bool> DeleteAsync(int id) =>
        (await http.DeleteAsync($"api/users/{id}")).IsSuccessStatusCode;

    public async Task<(bool Success, string? Error)> ToggleRoleAsync(int id)
    {
        var response = await http.PostAsync($"api/users/{id}/toggle-role", null);
        if (response.IsSuccessStatusCode)
            return (true, null);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        return (false, body?.Error);
    }
}
