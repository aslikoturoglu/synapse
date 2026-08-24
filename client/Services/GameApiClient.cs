using System.Net.Http.Json;

namespace Client.Services;

public class GameApiClient(HttpClient http)
{
    public async Task<int> GetBestScoreAsync()
    {
        try
        {
            var dto = await http.GetFromJsonAsync<GameScoreDto>("api/game/score");
            return dto?.BestScore ?? 0;
        }
        catch (HttpRequestException)
        {
            return 0;
        }
    }

    public async Task<int> SubmitScoreAsync(int score)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/game/score", new SubmitGameScoreRequest { Score = score });
            if (!response.IsSuccessStatusCode)
                return score;

            var dto = await response.Content.ReadFromJsonAsync<GameScoreDto>();
            return dto?.BestScore ?? score;
        }
        catch (HttpRequestException)
        {
            return score;
        }
    }
}
