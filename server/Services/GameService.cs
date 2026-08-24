using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Services;

public class GameService(AppDbContext db)
{
    public async Task<int> GetBestScoreAsync(int userId) =>
        await db.Users.Where(u => u.Id == userId).Select(u => u.BestGameScore).FirstOrDefaultAsync();

    // Re-checks server-side even though the client is only expected to call this when it
    // already believes Score beat BestScore — never lowers the stored value.
    public async Task<int> SubmitScoreAsync(int userId, int score)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null)
            return 0;

        if (score > user.BestGameScore)
        {
            user.BestGameScore = score;
            await db.SaveChangesAsync();
        }

        return user.BestGameScore;
    }
}
