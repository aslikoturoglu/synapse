using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Dtos;
using Server.Models;

namespace Server.Services;

public enum PostOpResult { Success, NotFound, Forbidden, GroupNotFound }

public class PostService(AppDbContext db)
{
    public async Task<List<PostDto>> GetFeedAsync() =>
        await QueryDto().OrderByDescending(p => p.CreatedDate).ThenByDescending(p => p.Id).ToListAsync();

    public async Task<List<PostDto>> GetMineAsync(int userId) =>
        await QueryDto().Where(p => p.AuthorId == userId)
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

    private IQueryable<PostDto> QueryDto() =>
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
