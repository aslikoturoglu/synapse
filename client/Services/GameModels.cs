namespace Client.Services;

public class GameScoreDto
{
    public int BestScore { get; set; }
}

public class SubmitGameScoreRequest
{
    public int Score { get; set; }
}
