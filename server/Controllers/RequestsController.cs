using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Dtos;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("api/requests")]
[Authorize]
public class RequestsController(RequestService requestService) : ControllerBase
{
    [Authorize(Roles = "Admin,Moderator")]
    [HttpGet]
    public async Task<ActionResult<List<UserRequestDto>>> GetAll() =>
        Ok(await requestService.GetAllAsync(User.IsInRole("Moderator"), User.GetUserId()));

    [HttpGet("mine")]
    public async Task<ActionResult<List<UserRequestDto>>> GetMine() =>
        Ok(await requestService.GetMineAsync(User.GetUserId()));

    [HttpPost]
    public async Task<ActionResult<UserRequestDto>> Create(CreateUserRequestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "Subject and content are required." });

        return Ok(await requestService.CreateAsync(User.GetUserId(), request.Subject.Trim(), request.Content.Trim()));
    }

    [HttpPost("role-change")]
    public async Task<ActionResult<UserRequestDto>> CreateRoleChange()
    {
        var result = await requestService.CreateRoleChangeAsync(User.GetUserId());
        return result is null ? BadRequest(new { error = "Can't request a role change for this account." }) : Ok(result);
    }

    [Authorize(Roles = "Admin,Moderator")]
    [HttpPost("{id:int}/seen")]
    public async Task<IActionResult> MarkSeen(int id) =>
        await requestService.MarkSeenAsync(id, User.IsInRole("Moderator")) ? NoContent() : NotFound();

    [Authorize(Roles = "Admin,Moderator")]
    [HttpPut("{id:int}/respond")]
    public async Task<IActionResult> Respond(int id, RespondToRequestRequest request)
    {
        var result = await requestService.RespondAsync(id, User.GetUserId(), User.IsInRole("Moderator"), request.Reply, request.Decision);
        return result switch
        {
            RespondResult.Success => NoContent(),
            RespondResult.NotFound => NotFound(),
            RespondResult.Forbidden => Forbid(),
            _ => BadRequest(new { error = "Invalid decision value." }),
        };
    }
}
