using System.Text.Json;
using Microsoft.JSInterop;

namespace Client.Services;

// Holds the signed-in session for the app's lifetime and mirrors it to localStorage so a
// page refresh doesn't log the user out. Registered as a singleton (same pattern as
// NotesStore/UiStrings in Program.cs).
public class AuthState(IJSRuntime js)
{
    private const string StorageKey = "synapse_auth";

    public string? Token { get; private set; }
    public UserDto? CurrentUser { get; private set; }
    public bool IsAuthenticated => Token is not null;

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        var json = await js.InvokeAsync<string?>("authStorage.getItem", StorageKey);
        if (string.IsNullOrEmpty(json))
            return;

        var stored = JsonSerializer.Deserialize<AuthResponseDto>(json);
        if (stored is null)
            return;

        Token = stored.Token;
        CurrentUser = stored.User;
        Changed?.Invoke();
    }

    public async Task SetSessionAsync(AuthResponseDto response)
    {
        Token = response.Token;
        CurrentUser = response.User;

        await js.InvokeVoidAsync("authStorage.setItem", StorageKey, JsonSerializer.Serialize(response));

        Changed?.Invoke();
    }

    public async Task ClearSessionAsync()
    {
        Token = null;
        CurrentUser = null;

        await js.InvokeVoidAsync("authStorage.removeItem", StorageKey);

        Changed?.Invoke();
    }
}
