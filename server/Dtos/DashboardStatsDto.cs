namespace Server.Dtos;

public class DashboardStatsDto
{
    public int AccountsCreated { get; set; }
    public int PostsCreated { get; set; }
    public int NotesCreated { get; set; }
    public int CommentsCreated { get; set; }

    // PostLikes has no per-row timestamp (it's an implicit EF many-to-many join table), so
    // this is always the all-time total regardless of the requested range — see
    // DashboardService.GetStatsAsync.
    public int LikesGiven { get; set; }

    // Post.Downloads/Sends exist on the model but nothing in the app increments them yet
    // (no download/share-tracking endpoint calls them) — always 0 today, not fabricated.
    public int Downloads { get; set; }
    public int Sends { get; set; }

    // Real per-user AI token metering isn't wired up yet (FoundryAgentClient doesn't capture
    // usage from the Responses API) — always 0 for now rather than a fabricated number.
    public int TokenUsage { get; set; }
    public double AverageTokenPerPerson { get; set; }
}
