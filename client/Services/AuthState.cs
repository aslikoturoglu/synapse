using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Client.Services;

// Holds the signed-in session for the app's lifetime and mirrors it to localStorage so a
// page refresh doesn't log the user out. Registered as a singleton (same pattern as
// NotesStore/UiStrings in Program.cs). Also keeps the shared HttpClient's Authorization
// header in sync with the token — without this, every [Authorize] endpoint besides
// login/signup silently 401s because nothing else ever sends the bearer token.
public class AuthState(IJSRuntime js, HttpClient http)
{
    private const string StorageKey = "synapse_auth";

    public string? Token { get; private set; }
    public UserDto? CurrentUser { get; private set; }
    public bool IsAuthenticated => Token is not null;
    public bool IsAdmin => CurrentUser?.Role == "Admin";

    // Granted by an Admin, never self-assigned. Keeps full normal-user content permissions
    // (unlike Admin) plus most of Admin's All Users management powers — just not the ability
    // to change anyone's role. UI that's specifically about managing other accounts should
    // check CanManageUsers; UI about the caller's own content permissions should keep checking
    // IsAdmin alone, since a Moderator is meant to look just like a User there.
    public bool IsModerator => CurrentUser?.Role == "Moderator";
    public bool CanManageUsers => IsAdmin || IsModerator;

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
        ApplyAuthHeader();
        Changed?.Invoke();
    }

    public async Task SetSessionAsync(AuthResponseDto response)
    {
        Token = response.Token;
        CurrentUser = response.User;
        ApplyAuthHeader();

        await js.InvokeVoidAsync("authStorage.setItem", StorageKey, JsonSerializer.Serialize(response));

        Changed?.Invoke();
    }

    public async Task ClearSessionAsync()
    {
        Token = null;
        CurrentUser = null;
        ApplyAuthHeader();

        await js.InvokeVoidAsync("authStorage.removeItem", StorageKey);

        Changed?.Invoke();
    }

    private void ApplyAuthHeader() =>
        http.DefaultRequestHeaders.Authorization = Token is null ? null : new AuthenticationHeaderValue("Bearer", Token);
}
