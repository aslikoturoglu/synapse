using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Dtos;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("api/connections")]
[Authorize]
public class ConnectionsController(ConnectionService connectionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ConnectionDto>>> GetMine() => Ok(await connectionService.GetConnectionsAsync(User.GetUserId()));

    [HttpPost("{userId:int}/toggle")]
    public async Task<ActionResult<FollowResponse>> ToggleFollow(int userId)
    {
        var (result, following) = await connectionService.ToggleFollowAsync(User.GetUserId(), userId);
        return result switch
        {
            FollowOpResult.NotFound => NotFound(),
            FollowOpResult.SelfFollow => BadRequest(new { error = "You can't follow yourself." }),
            _ => Ok(new FollowResponse { Following = following }),
        };
    }
}
