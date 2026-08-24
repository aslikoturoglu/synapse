using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Dtos;
using Server.Services;

namespace Server.Controllers;

// Its own controller, not folded into UsersController — that one is
// [Authorize(Roles = "Admin,Moderator")] at the class level, which would wrongly restrict
// a "my own score" endpoint to staff only.
[ApiController]
[Route("api/game")]
[Authorize]
public class GameController(GameService gameService) : ControllerBase
{
    [HttpGet("score")]
    public async Task<ActionResult<GameScoreDto>> GetBestScore() =>
        Ok(new GameScoreDto { BestScore = await gameService.GetBestScoreAsync(User.GetUserId()) });

    [HttpPost("score")]
    public async Task<ActionResult<GameScoreDto>> SubmitScore(SubmitGameScoreRequest request) =>
        Ok(new GameScoreDto { BestScore = await gameService.SubmitScoreAsync(User.GetUserId(), request.Score) });
}
