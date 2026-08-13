using System.Net.Http.Json;

namespace Client.Services;

public record AuthResult(bool Succeeded, string? Error, AuthResponseDto? Response)
{
    public static AuthResult Ok(AuthResponseDto response) => new(true, null, response);
    public static AuthResult Fail(string error) => new(false, error, null);
}

public class AuthApiClient(HttpClient http)
{
    public Task<AuthResult> SignupAsync(SignupRequest request) => PostAsync("api/auth/signup", request);

    public Task<AuthResult> LoginAsync(LoginRequest request) => PostAsync("api/auth/login", request);

    private async Task<AuthResult> PostAsync<TRequest>(string url, TRequest request)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync(url, request);
        }
        catch (HttpRequestException)
        {
            return AuthResult.Fail("Could not reach the server. Please try again.");
        }

        if (response.IsSuccessStatusCode)
        {
            var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return auth is null ? AuthResult.Fail("Unexpected server response.") : AuthResult.Ok(auth);
        }

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        return AuthResult.Fail(error?.Error ?? "Something went wrong. Please try again.");
    }

    private class ErrorResponse
    {
        public string? Error { get; set; }
    }
}
