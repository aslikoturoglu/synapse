using System.Net.Http.Json;

namespace Client.Services;

public class DashboardApiClient(HttpClient http)
{
    public async Task<DashboardStatsDto?> GetStatsAsync(string range, DateTime? from = null, DateTime? to = null)
    {
        var url = $"api/dashboard?range={Uri.EscapeDataString(range)}";
        if (from is not null)
            url += $"&from={Uri.EscapeDataString(from.Value.ToString("o"))}";
        if (to is not null)
            url += $"&to={Uri.EscapeDataString(to.Value.ToString("o"))}";

        return await http.GetFromJsonAsync<DashboardStatsDto>(url);
    }
}
