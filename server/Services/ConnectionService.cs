using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Dtos;
using Server.Models;

namespace Server.Services;

public enum FollowOpResult { Success, NotFound, SelfFollow }

public class ConnectionService(AppDbContext db)
{
    public async Task<(FollowOpResult Result, bool Following)> ToggleFollowAsync(int userId, int targetUserId)
    {
        if (userId == targetUserId)
            return (FollowOpResult.SelfFollow, false);

        if (!await db.Users.AnyAsync(u => u.Id == targetUserId))
            return (FollowOpResult.NotFound, false);

        var existing = await db.Follows.FirstOrDefaultAsync(f => f.FollowerId == userId && f.FollowingId == targetUserId);
        if (existing is not null)
        {
            db.Follows.Remove(existing);
            await db.SaveChangesAsync();
            return (FollowOpResult.Success, false);
        }

        db.Follows.Add(new Follow { FollowerId = userId, FollowingId = targetUserId });
        await db.SaveChangesAsync();
        return (FollowOpResult.Success, true);
    }

    // The caller's own "Connections": everyone they follow, alphabetical by name.
    public async Task<List<ConnectionDto>> GetConnectionsAsync(int userId) =>
        await db.Follows
            .Where(f => f.FollowerId == userId)
            .OrderBy(f => f.Following.Name).ThenBy(f => f.Following.Surname)
            .Select(f => new ConnectionDto
            {
                Id = f.FollowingId,
                Name = f.Following.Name,
                Surname = f.Following.Surname,
                JobTitle = f.Following.JobTitle,
            })
            .ToListAsync();
}
