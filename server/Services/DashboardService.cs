using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Dtos;

namespace Server.Services;

public class DashboardService(AppDbContext db, TokenUsageService tokenUsageService)
{
    // start == null means "no lower bound" (the "Total" range). end defaults to now for every
    // preset range; "custom" trusts the caller's own from/to.
    public static (DateTime? Start, DateTime End) ResolveRange(string range, DateTime? from, DateTime? to)
    {
        var end = to ?? DateTime.UtcNow;
        DateTime? start = range switch
        {
            "6h" => end.AddHours(-6),
            "24h" => end.AddHours(-24),
            "1w" => end.AddDays(-7),
            "1m" => end.AddMonths(-1),
            "6m" => end.AddMonths(-6),
            "1y" => end.AddYears(-1),
            "custom" => from,
            _ => null,
        };
        return (start, end);
    }

    public async Task<DashboardStatsDto> GetStatsAsync(DateTime? start, DateTime end)
    {
        var accounts = await db.Users.CountAsync(u => u.CreatedAt <= end && (start == null || u.CreatedAt >= start));
        var posts = await db.Posts.CountAsync(p => p.IsShared && p.CreatedAt <= end && (start == null || p.CreatedAt >= start));
        var notes = await db.Posts.CountAsync(p => p.CreatedAt <= end && (start == null || p.CreatedAt >= start));
        var comments = await db.Comments.CountAsync(c => c.CreatedAt <= end && (start == null || c.CreatedAt >= start));

        // No per-row timestamp on the PostLikes join table — always the all-time total,
        // regardless of the requested range (see DashboardStatsDto.LikesGiven).
        var likes = await db.Posts.SelectMany(p => p.LikedByUsers).CountAsync();

        var downloads = await db.Posts.SumAsync(p => (int?)p.Downloads) ?? 0;
        var sends = await db.Posts.SumAsync(p => (int?)p.Sends) ?? 0;
        var tokenUsage = await tokenUsageService.GetRangedTotalAsync(start, end);

        return new DashboardStatsDto
        {
            AccountsCreated = accounts,
            PostsCreated = posts,
            NotesCreated = notes,
            CommentsCreated = comments,
            LikesGiven = likes,
            Downloads = downloads,
            Sends = sends,
            TokenUsage = tokenUsage,
            AverageTokenPerPerson = accounts > 0 ? tokenUsage / (double)accounts : 0,
        };
    }
}
