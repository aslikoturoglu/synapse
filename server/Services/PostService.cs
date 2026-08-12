using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Dtos;
using Server.Models;

namespace Server.Services;

public enum PostOpResult { Success, NotFound, Forbidden, GroupNotFound }

public class PostService(AppDbContext db)
{
    public async Task<List<PostDto>> GetFeedAsync(int userId) =>
        await QueryDto(userId).OrderByDescending(p => p.CreatedDate).ThenByDescending(p => p.Id).ToListAsync();

    public async Task<List<PostDto>> GetMineAsync(int userId) =>
        await QueryDto(userId).Where(p => p.AuthorId == userId)
            .OrderByDescending(p => p.CreatedDate).ThenByDescending(p => p.Id)
            .ToListAsync();

    public async Task<PostDto?> CreateAsync(int userId, CreatePostRequest request)
    {
        var author = await db.Users.FindAsync(userId);
        if (author is null)
            return null;

        var post = new Post
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            MiniDescription = request.MiniDescription.Trim(),
            CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            AuthorId = userId,
        };

        foreach (var file in request.Files)
            post.Files.Add(new NoteFile { FileName = file.FileName, FileUrl = file.FileUrl, PageCount = file.PageCount });

        db.Posts.Add(post);
        await db.SaveChangesAsync();

        return ToDto(post, author);
    }

    public async Task<PostOpResult> DeleteAsync(int userId, int postId)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null)
            return PostOpResult.NotFound;
        if (post.AuthorId != userId)
            return PostOpResult.Forbidden;

        db.Posts.Remove(post);
        await db.SaveChangesAsync();
        return PostOpResult.Success;
    }

    public async Task<PostOpResult> SetGroupAsync(int userId, int postId, int? groupId)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null)
            return PostOpResult.NotFound;
        if (post.AuthorId != userId)
            return PostOpResult.Forbidden;

        if (groupId is int id)
        {
            var group = await db.Groups.FindAsync(id);
            if (group is null || group.OwnerId != userId)
                return PostOpResult.GroupNotFound;
        }

        post.GroupId = groupId;
        await db.SaveChangesAsync();
        return PostOpResult.Success;
    }

    public Task<(PostOpResult Status, bool Active, int Count)> ToggleLikeAsync(int userId, int postId) =>
        ToggleReactionAsync(userId, postId, nameof(Post.LikedByUsers), p => p.LikedByUsers);

    public Task<(PostOpResult Status, bool Active, int Count)> ToggleFavoriteAsync(int userId, int postId) =>
        ToggleReactionAsync(userId, postId, nameof(Post.FavoritedByUsers), p => p.FavoritedByUsers);

    public Task<(PostOpResult Status, bool Active, int Count)> ToggleRepostAsync(int userId, int postId) =>
        ToggleReactionAsync(userId, postId, nameof(Post.RepostedByUsers), p => p.RepostedByUsers);

    public async Task<PostCommentDto?> AddCommentAsync(int userId, int postId, string text)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null)
            return null;

        var author = await db.Users.FindAsync(userId);
        if (author is null)
            return null;

        var comment = new PostComment { PostId = postId, AuthorId = userId, Text = text.Trim() };
        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        return new PostCommentDto
        {
            Id = comment.Id,
            AuthorId = userId,
            AuthorName = $"{author.Name} {author.Surname}",
            Text = comment.Text,
            CreatedAt = comment.CreatedAt,
        };
    }

    public async Task<PostOpResult> DeleteCommentAsync(int userId, bool isAdmin, int commentId)
    {
        var comment = await db.Comments.FindAsync(commentId);
        if (comment is null)
            return PostOpResult.NotFound;
        if (comment.AuthorId != userId && !isAdmin)
            return PostOpResult.Forbidden;

        db.Comments.Remove(comment);
        await db.SaveChangesAsync();
        return PostOpResult.Success;
    }

    // Toggles the caller's membership in a post's Liked/Favorited/Reposted user list. Loading
    // both the post's reaction collection and the user through the same DbContext relies on
    // EF's identity map so `list.Remove(user)` finds the tracked instance by reference.
    private async Task<(PostOpResult Status, bool Active, int Count)> ToggleReactionAsync(
        int userId, int postId, string includePath, Func<Post, List<User>> reactionList)
    {
        var post = await db.Posts.Include(includePath).FirstOrDefaultAsync(p => p.Id == postId);
        if (post is null)
            return (PostOpResult.NotFound, false, 0);

        var user = await db.Users.FindAsync(userId);
        if (user is null)
            return (PostOpResult.NotFound, false, 0);

        var list = reactionList(post);
        bool active;
        if (list.Remove(user))
            active = false;
        else
        {
            list.Add(user);
            active = true;
        }

        await db.SaveChangesAsync();
        return (PostOpResult.Success, active, list.Count);
    }

    private IQueryable<PostDto> QueryDto(int currentUserId) =>
        db.Posts.Select(p => new PostDto
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            MiniDescription = p.MiniDescription,
            CreatedDate = p.CreatedDate,
            AuthorId = p.AuthorId,
            AuthorName = p.Author.Name + " " + p.Author.Surname,
            AuthorRole = p.Author.JobTitle,
            GroupId = p.GroupId,
            GroupName = p.Group != null ? p.Group.Name : null,
            Sends = p.Sends,
            Downloads = p.Downloads,
            Files = p.Files
                .Select(f => new NoteFileDto { FileName = f.FileName, FileUrl = f.FileUrl, PageCount = f.PageCount })
                .ToList(),
            LikeCount = p.LikedByUsers.Count,
            FavoriteCount = p.FavoritedByUsers.Count,
            RepostCount = p.RepostedByUsers.Count,
            CommentCount = p.Comments.Count,
            LikedByMe = p.LikedByUsers.Any(u => u.Id == currentUserId),
            FavoritedByMe = p.FavoritedByUsers.Any(u => u.Id == currentUserId),
            RepostedByMe = p.RepostedByUsers.Any(u => u.Id == currentUserId),
            Comments = p.Comments
                .OrderBy(c => c.CreatedAt)
                .Select(c => new PostCommentDto
                {
                    Id = c.Id,
                    AuthorId = c.AuthorId,
                    AuthorName = c.Author.Name + " " + c.Author.Surname,
                    Text = c.Text,
                    CreatedAt = c.CreatedAt,
                })
                .ToList(),
        });

    private static PostDto ToDto(Post post, User author) => new()
    {
        Id = post.Id,
        Title = post.Title,
        Description = post.Description,
        MiniDescription = post.MiniDescription,
        CreatedDate = post.CreatedDate,
        AuthorId = post.AuthorId,
        AuthorName = $"{author.Name} {author.Surname}",
        AuthorRole = author.JobTitle,
        GroupId = post.GroupId,
        Files = post.Files
            .Select(f => new NoteFileDto { FileName = f.FileName, FileUrl = f.FileUrl, PageCount = f.PageCount })
            .ToList(),
    };
}
