using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Dtos;
using Server.Models;

namespace Server.Services;

// Public-profile reads for any signed-in viewer looking at *someone else's* profile — distinct
// from ConnectionService (the caller's own following list) and the admin-only UserService.
// Name is not a unique key (two accounts can share the exact same "Name Surname" — the client's
// existing /profile/{name} routing already has this limitation), so every lookup here has an
// id-based form; callers should use it whenever they already know the id (e.g. from one of that
// author's posts) and only fall back to the name-based form when they don't.
public class ProfileService(AppDbContext db)
{
    public async Task<ProfileSummaryDto?> GetSummaryByIdAsync(int viewerId, int userId)
    {
        var user = await db.Users.FindAsync(userId);
        return user is null ? null : await BuildSummaryAsync(viewerId, user);
    }

    public async Task<ProfileSummaryDto?> GetSummaryAsync(int viewerId, string fullName)
    {
        var user = await FindByNameAsync(fullName);
        return user is null ? null : await BuildSummaryAsync(viewerId, user);
    }

    public Task<List<ConnectionDto>> GetFollowersByIdAsync(int userId) =>
        db.Follows.Where(f => f.FollowingId == userId)
            .OrderBy(f => f.Follower.Name).ThenBy(f => f.Follower.Surname)
            .Select(f => new ConnectionDto { Id = f.FollowerId, Name = f.Follower.Name, Surname = f.Follower.Surname, JobTitle = f.Follower.JobTitle })
            .ToListAsync();

    public async Task<List<ConnectionDto>?> GetFollowersAsync(string fullName)
    {
        var user = await FindByNameAsync(fullName);
        return user is null ? null : await GetFollowersByIdAsync(user.Id);
    }

    public Task<List<ConnectionDto>> GetFollowingByIdAsync(int userId) =>
        db.Follows.Where(f => f.FollowerId == userId)
            .OrderBy(f => f.Following.Name).ThenBy(f => f.Following.Surname)
            .Select(f => new ConnectionDto { Id = f.FollowingId, Name = f.Following.Name, Surname = f.Following.Surname, JobTitle = f.Following.JobTitle })
            .ToListAsync();

    public async Task<List<ConnectionDto>?> GetFollowingAsync(string fullName)
    {
        var user = await FindByNameAsync(fullName);
        return user is null ? null : await GetFollowingByIdAsync(user.Id);
    }

    private async Task<ProfileSummaryDto> BuildSummaryAsync(int viewerId, User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Surname = user.Surname,
        Username = user.Username,
        JobTitle = user.JobTitle,
        CreatedAt = user.CreatedAt,
        PostCount = await db.Posts.CountAsync(p => p.AuthorId == user.Id && p.IsShared),
        FollowerCount = await db.Follows.CountAsync(f => f.FollowingId == user.Id),
        FollowingCount = await db.Follows.CountAsync(f => f.FollowerId == user.Id),
        FollowedByMe = await db.Follows.AnyAsync(f => f.FollowerId == viewerId && f.FollowingId == user.Id),
    };

    private Task<User?> FindByNameAsync(string fullName) =>
        db.Users.FirstOrDefaultAsync(u => (u.Name + " " + u.Surname) == fullName);
}
