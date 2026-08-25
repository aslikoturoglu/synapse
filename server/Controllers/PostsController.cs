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
        // Admin is a moderation role, not a participant — same reasoning as the like/comment/
        // share guards below.
        if (User.IsInRole("Admin"))
            return Forbid();

        var post = await postService.CreateAsync(User.GetUserId(), request);
        if (post is null)
            return Unauthorized();

        return CreatedAtAction(nameof(GetFeed), post);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PostDetailDto>> GetDetail(int id)
    {
        var post = await postService.GetDetailAsync(User.GetUserId(), id);
        return post is null ? NotFound() : Ok(post);
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> GetPdf(int id)
    {
        var result = await postService.GenerateNotePdfAsync(User.GetUserId(), id);
        return result is null ? NotFound() : File(result.Value.Bytes, "application/pdf", $"{result.Value.Title}.pdf");
    }

    [HttpPost("{id:int}/keywords")]
    public async Task<ActionResult<BrainMapKeywordDto>> AddKeyword(int id, AddKeywordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { error = "Keyword text is required." });

        var keyword = await postService.AddKeywordAsync(User.GetUserId(), id, request.Text);
        return keyword is null ? Forbid() : Ok(keyword);
    }

    [HttpDelete("{id:int}/keywords/{keywordId:int}")]
    public async Task<IActionResult> RemoveKeyword(int id, int keywordId)
    {
        var result = await postService.RemoveKeywordAsync(User.GetUserId(), id, keywordId);
        return result switch
        {
            PostOpResult.Success => NoContent(),
            PostOpResult.NotFound => NotFound(),
            PostOpResult.Forbidden => Forbid(),
            _ => BadRequest(),
        };
    }

    [HttpPost("{id:int}/keywords/{keywordId:int}/restore")]
    public async Task<IActionResult> RestoreKeyword(int id, int keywordId)
    {
        var result = await postService.RestoreKeywordAsync(User.GetUserId(), id, keywordId);
        return result switch
        {
            PostOpResult.Success => NoContent(),
            PostOpResult.NotFound => NotFound(),
            PostOpResult.Forbidden => Forbid(),
            _ => BadRequest(),
        };
    }

    [HttpPut("{id:int}/title")]
    public async Task<IActionResult> UpdateTitle(int id, UpdatePostTitleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { error = "Title is required." });

        var result = await postService.UpdateTitleAsync(User.GetUserId(), id, request.Title.Trim());
        return result switch
        {
            PostOpResult.Success => NoContent(),
            PostOpResult.NotFound => NotFound(),
            PostOpResult.Forbidden => Forbid(),
            _ => BadRequest(),
        };
    }

    [HttpPut("{id:int}/description")]
    public async Task<IActionResult> UpdateDescription(int id, UpdatePostDescriptionRequest request)
    {
        var result = await postService.UpdateDescriptionAsync(User.GetUserId(), id, request.Description.Trim());
        return result switch
        {
            PostOpResult.Success => NoContent(),
            PostOpResult.NotFound => NotFound(),
            PostOpResult.Forbidden => Forbid(),
            _ => BadRequest(),
        };
    }

    [HttpPut("{id:int}/share-settings")]
    public async Task<IActionResult> UpdateShareSettings(int id, UpdateShareSettingsRequest request)
    {
        var result = await postService.UpdateShareSettingsAsync(User.GetUserId(), id, request.ShareBrainMap, request.ShareProcess);
        return result switch
        {
            PostOpResult.Success => NoContent(),
            PostOpResult.NotFound => NotFound(),
            PostOpResult.Forbidden => Forbid(),
            _ => BadRequest(),
        };
    }

    [HttpPut("{id:int}/pages/{pageNumber:int}")]
    public async Task<IActionResult> UpdatePageBody(int id, int pageNumber, UpdatePageBodyRequest request)
    {
        var result = await postService.UpdatePageBodyAsync(User.GetUserId(), id, pageNumber, request.Body);
        return result switch
        {
            PostOpResult.Success => NoContent(),
            PostOpResult.NotFound => NotFound(),
            PostOpResult.Forbidden => Forbid(),
            _ => BadRequest(),
        };
    }

    [HttpPut("{id:int}/personal-notes")]
    public async Task<IActionResult> UpdatePersonalNotes(int id, UpdatePersonalNotesRequest request)
    {
        var result = await postService.UpdatePersonalNotesAsync(User.GetUserId(), id, request.Notes);
        return result switch
        {
            PostOpResult.Success => NoContent(),
            PostOpResult.NotFound => NotFound(),
            PostOpResult.Forbidden => Forbid(),
            _ => BadRequest(),
        };
    }

    [HttpPut("{id:int}/pages/{pageNumber:int}/heading")]
    public async Task<IActionResult> UpdatePageHeading(int id, int pageNumber, UpdatePageHeadingRequest request)
    {
        var result = await postService.UpdatePageHeadingAsync(User.GetUserId(), id, pageNumber, request.Heading);
        return result switch
        {
            PostOpResult.Success => NoContent(),
            PostOpResult.NotFound => NotFound(),
            PostOpResult.Forbidden => Forbid(),
            _ => BadRequest(),
        };
    }

    [HttpPost("{id:int}/document-change")]
    public async Task<IActionResult> IncrementDocumentChange(int id)
    {
        var result = await postService.IncrementDocumentChangeAsync(User.GetUserId(), id);
        return result switch
        {
            PostOpResult.Success => NoContent(),
            PostOpResult.NotFound => NotFound(),
            PostOpResult.Forbidden => Forbid(),
            _ => BadRequest(),
        };
    }

    [HttpPost("{id:int}/highlights/{highlightId:guid}/messages/{messageId:int}/add-to-document")]
    public async Task<IActionResult> MarkAddedToDocument(int id, Guid highlightId, int messageId)
    {
        var result = await postService.MarkAddedToDocumentAsync(User.GetUserId(), id, highlightId, messageId);
        return result switch
        {
            PostOpResult.Success => NoContent(),
            PostOpResult.NotFound => NotFound(),
            PostOpResult.Forbidden => Forbid(),
            _ => BadRequest(),
        };
    }

    [HttpPost("{id:int}/highlights")]
    public async Task<ActionResult<NoteHighlightDto>> CreateHighlight(int id, CreateHighlightRequest request)
    {
        var highlight = await postService.CreateHighlightAsync(User.GetUserId(), id, request);
        return highlight is null ? Forbid() : Ok(highlight);
    }

    [HttpPost("{id:int}/highlights/{highlightId:guid}/messages")]
    public async Task<ActionResult<AiChatMessageDto>> AddHighlightMessage(int id, Guid highlightId, AddHighlightMessageRequest request)
    {
        var message = await postService.AddHighlightMessageAsync(User.GetUserId(), highlightId, request);
        return message is null ? Forbid() : Ok(message);
    }

    [HttpPost("{id:int}/ask")]
    public async Task<ActionResult<AskAiResponse>> Ask(int id, AskAiRequest request)
    {
        var result = await postService.AskGeneralAsync(User.GetUserId(), id, request.ThreadId, request.Question);
        return result is null ? Forbid() : Ok(new AskAiResponse { Answer = result.Value.Answer, ThreadId = result.Value.ThreadId });
    }

    [HttpPost("{id:int}/map")]
    public async Task<ActionResult<GraphDto>> GetMap(int id)
    {
        var graph = await postService.GetOrGenerateMapAsync(User.GetUserId(), id);
        return graph is null ? NotFound() : Ok(graph);
    }

    [HttpPost("{id:int}/map/regenerate")]
    public async Task<ActionResult<GraphDto>> RegenerateMap(int id)
    {
        var graph = await postService.RegenerateMapAsync(User.GetUserId(), id);
        return graph is null ? NotFound() : Ok(graph);
    }

    [HttpPost("{id:int}/share")]
    public async Task<IActionResult> Share(int id)
    {
        if (User.IsInRole("Admin"))
            return Forbid();

        var result = await postService.ShareAsync(User.GetUserId(), id);
        return result switch
        {
            PostOpResult.Success => NoContent(),
            PostOpResult.NotFound => NotFound(),
            PostOpResult.Forbidden => Forbid(),
            _ => BadRequest(),
        };
    }

    [HttpDelete("{id:int}/share")]
    public async Task<IActionResult> Unshare(int id)
    {
        var result = await postService.UnshareAsync(User.GetUserId(), id);
        return result switch
        {
            PostOpResult.Success => NoContent(),
            PostOpResult.NotFound => NotFound(),
            PostOpResult.Forbidden => Forbid(),
            _ => BadRequest(),
        };
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
    public Task<ActionResult> ToggleLike(int id) =>
        User.IsInRole("Admin") ? Task.FromResult<ActionResult>(Forbid()) : ToggleReactionAsync(postService.ToggleLikeAsync, id);

    [HttpPost("{id:int}/favorite")]
    public Task<ActionResult> ToggleFavorite(int id) =>
        User.IsInRole("Admin") ? Task.FromResult<ActionResult>(Forbid()) : ToggleReactionAsync(postService.ToggleFavoriteAsync, id);

    [HttpPost("{id:int}/repost")]
    public Task<ActionResult> ToggleRepost(int id) => ToggleReactionAsync(postService.ToggleRepostAsync, id);

    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<PostCommentDto>> AddComment(int id, AddCommentRequest request)
    {
        if (User.IsInRole("Admin"))
            return Forbid();

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
