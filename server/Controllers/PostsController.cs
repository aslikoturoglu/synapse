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
    public async Task<ActionResult<List<PostDto>>> GetFeed() => Ok(await postService.GetFeedAsync(User.GetUserId()));

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

    [HttpPost("{id:int}/like")]
    public Task<ActionResult> ToggleLike(int id) => ToggleReactionAsync(postService.ToggleLikeAsync, id);

    [HttpPost("{id:int}/favorite")]
    public Task<ActionResult> ToggleFavorite(int id) => ToggleReactionAsync(postService.ToggleFavoriteAsync, id);

    [HttpPost("{id:int}/repost")]
    public Task<ActionResult> ToggleRepost(int id) => ToggleReactionAsync(postService.ToggleRepostAsync, id);

    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<PostCommentDto>> AddComment(int id, AddCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { error = "Comment text is required." });

        var comment = await postService.AddCommentAsync(User.GetUserId(), id, request.Text);
        return comment is null ? NotFound() : Ok(comment);
    }

    [HttpDelete("comments/{commentId:int}")]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var result = await postService.DeleteCommentAsync(User.GetUserId(), User.IsInRole("Admin"), commentId);
        return result switch
        {
            PostOpResult.Success => NoContent(),
            PostOpResult.NotFound => NotFound(),
            PostOpResult.Forbidden => Forbid(),
            _ => BadRequest(),
        };
    }

    private async Task<ActionResult> ToggleReactionAsync(Func<int, int, Task<(PostOpResult Status, bool Active, int Count)>> toggle, int postId)
    {
        var (status, active, count) = await toggle(User.GetUserId(), postId);
        return status == PostOpResult.NotFound ? NotFound() : Ok(new ReactionResponse { Active = active, Count = count });
    }
}
