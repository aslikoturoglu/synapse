using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Dtos;
using Server.Services;

namespace Server.Controllers;

// Public-profile reads. Prefer the by-id routes whenever the caller already knows the id (e.g.
// from one of that author's posts) — name is not unique (see ProfileService), so the /{name}
// routes below can resolve to the wrong account when two users share a display name.
[ApiController]
[Route("api/profiles")]
[Authorize]
public class ProfilesController(ProfileService profileService, PostService postService) : ControllerBase
{
    [HttpGet("by-id/{userId:int}")]
    public async Task<ActionResult<ProfileSummaryDto>> GetSummaryById(int userId)
    {
        var summary = await profileService.GetSummaryByIdAsync(User.GetUserId(), userId);
        return summary is null ? NotFound() : Ok(summary);
    }

    [HttpGet("by-id/{userId:int}/followers")]
    public async Task<ActionResult<List<ConnectionDto>>> GetFollowersById(int userId) =>
        Ok(await profileService.GetFollowersByIdAsync(userId));

    [HttpGet("by-id/{userId:int}/following")]
    public async Task<ActionResult<List<ConnectionDto>>> GetFollowingById(int userId) =>
        Ok(await profileService.GetFollowingByIdAsync(userId));

    [HttpGet("by-id/{userId:int}/reposts")]
    public async Task<ActionResult<List<PostDto>>> GetRepostsById(int userId) =>
        Ok(await postService.GetRepostsByUserAsync(User.GetUserId(), userId));

    [HttpGet("{name}")]
    public async Task<ActionResult<ProfileSummaryDto>> GetSummary(string name)
    {
        var summary = await profileService.GetSummaryAsync(User.GetUserId(), name);
        return summary is null ? NotFound() : Ok(summary);
    }

    [HttpGet("{name}/followers")]
    public async Task<ActionResult<List<ConnectionDto>>> GetFollowers(string name)
    {
        var followers = await profileService.GetFollowersAsync(name);
        return followers is null ? NotFound() : Ok(followers);
    }

    [HttpGet("{name}/following")]
    public async Task<ActionResult<List<ConnectionDto>>> GetFollowing(string name)
    {
        var following = await profileService.GetFollowingAsync(name);
        return following is null ? NotFound() : Ok(following);
    }

    [HttpGet("{name}/reposts")]
    public async Task<ActionResult<List<PostDto>>> GetReposts(string name)
    {
        var summary = await profileService.GetSummaryAsync(User.GetUserId(), name);
        if (summary is null)
            return NotFound();

        return Ok(await postService.GetRepostsByUserAsync(User.GetUserId(), summary.Id));
    }
}
