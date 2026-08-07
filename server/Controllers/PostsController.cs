using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Dtos;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("api/posts")]
[Authorize]
public class PostsController(PostService postService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PostDto>>> GetFeed() => Ok(await postService.GetFeedAsync());

    [HttpGet("mine")]
    public async Task<ActionResult<List<PostDto>>> GetMine() => Ok(await postService.GetMineAsync(User.GetUserId()));

    [HttpPost]
    public async Task<ActionResult<PostDto>> Create(CreatePostRequest request)
    {
        var post = await postService.CreateAsync(User.GetUserId(), request);
        if (post is null)
            return Unauthorized();

        return CreatedAtAction(nameof(GetFeed), post);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await postService.DeleteAsync(User.GetUserId(), id);
        return result switch
        {
            PostOpResult.Success => NoContent(),
            PostOpResult.NotFound => NotFound(),
            PostOpResult.Forbidden => Forbid(),
            _ => BadRequest(),
        };
    }

    [HttpPatch("{id:int}/group")]
    public async Task<IActionResult> SetGroup(int id, UpdatePostGroupRequest request)
    {
        var result = await postService.SetGroupAsync(User.GetUserId(), id, request.GroupId);
        return result switch
        {
            PostOpResult.Success => NoContent(),
            PostOpResult.NotFound => NotFound(),
            PostOpResult.Forbidden => Forbid(),
            PostOpResult.GroupNotFound => BadRequest(new { error = "Group not found." }),
            _ => BadRequest(),
        };
    }
}
