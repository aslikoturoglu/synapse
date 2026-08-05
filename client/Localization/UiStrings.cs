using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;

namespace Client.Localization;

// Centralizes UI text that repeats across pages/components (nav labels, common button
// labels, common placeholders) so it's defined once in wwwroot/strings.json instead of
// copy-pasted everywhere. Not a full i18n system — one-off/unique content (mock post text,
// validation messages, etc.) intentionally stays inline in its Razor file.
public class UiStrings
{
    private readonly NavigationManager _navigation;
    private readonly HttpClient _http = new();
    private Dictionary<string, string> _values = [];

    public UiStrings(NavigationManager navigation)
    {
        _navigation = navigation;
    }

    public async Task LoadAsync()
    {
        var url = new Uri(new Uri(_navigation.BaseUri), "strings.json");
        _values = await _http.GetFromJsonAsync<Dictionary<string, string>>(url) ?? [];
    }

    public string Get(string key) => _values.TryGetValue(key, out var value) ? value : key;
}
