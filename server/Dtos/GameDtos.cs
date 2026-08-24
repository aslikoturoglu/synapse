namespace Server.Dtos;

public class GameScoreDto
{
    public required int BestScore { get; set; }
}

public class SubmitGameScoreRequest
{
    public required int Score { get; set; }
}
