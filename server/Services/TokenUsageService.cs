using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Services;

public class TokenUsageService(AppDbContext db)
{
    public async Task RecordAsync(int userId, int tokens)
    {
        db.TokenUsageLogs.Add(new TokenUsageLog { UserId = userId, Tokens = tokens });
        await db.SaveChangesAsync();
    }

    // AllUsers' per-user column — lifetime, no date range.
    public async Task<Dictionary<int, int>> GetLifetimeTotalsByUserAsync() =>
        await db.TokenUsageLogs
            .GroupBy(l => l.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(l => l.Tokens) })
            .ToDictionaryAsync(x => x.UserId, x => x.Total);

    // Dashboard's stat — same start/end range every other DashboardService number uses.
    public async Task<int> GetRangedTotalAsync(DateTime? start, DateTime end) =>
        await db.TokenUsageLogs
            .Where(l => l.CreatedAt <= end && (start == null || l.CreatedAt >= start))
            .SumAsync(l => (int?)l.Tokens) ?? 0;
}
