using System.Net;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Client;
using Client.Localization;
using Client.Pages.Home;
using Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl is not configured.");
// Singleton, not Scoped: a standalone WASM app has one root scope for its whole lifetime
// anyway, and AuthState (Singleton) needs to hold this same HttpClient to keep its
// Authorization header in sync — a Scoped HttpClient under a Singleton fails DI validation.
builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

builder.Services.AddSingleton<NotesStore>();
builder.Services.AddSingleton<UiStrings>();
builder.Services.AddSingleton<AuthApiClient>();
builder.Services.AddSingleton<PostsApiClient>();
builder.Services.AddSingleton<AiApiClient>();
builder.Services.AddSingleton<AuthState>();

var host = builder.Build();

await host.Services.GetRequiredService<UiStrings>().LoadAsync();

var authState = host.Services.GetRequiredService<AuthState>();
await authState.InitializeAsync();
if (authState.CurrentUser is { } currentUser)
{
    var notesStore = host.Services.GetRequiredService<NotesStore>();
    notesStore.ApplyAuthUser(currentUser);
    try
    {
        await notesStore.LoadFeedAsync();
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
    {
        // The token restored from localStorage has expired or is otherwise no longer
        // valid server-side (JWTs here live 60 minutes) — drop it so the app boots into a
        // clean logged-out state instead of crashing on an unhandled 401.
        await authState.ClearSessionAsync();
    }
}

await host.RunAsync();
