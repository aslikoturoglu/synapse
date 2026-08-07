using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Dtos;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("api/groups")]
[Authorize]
public class GroupsController(GroupService groupService) : ControllerBase
{
    [HttpGet("mine")]
    public async Task<ActionResult<List<GroupDto>>> GetMine() => Ok(await groupService.GetMineAsync(User.GetUserId()));

    [HttpPost]
    public async Task<ActionResult<GroupDto>> Create(CreateGroupRequest request)
    {
        var (result, group) = await groupService.CreateAsync(User.GetUserId(), request.Name);
        if (result == GroupOpResult.Conflict)
            return Conflict(new { error = "A group with this name already exists." });

        return CreatedAtAction(nameof(GetMine), group);
    }
}
