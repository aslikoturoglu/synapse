namespace Server.Services;

// Scoped — one instance per HTTP request. Every *AiService adds to this as it makes AskAsync
// calls; TokenUsageActionFilter reads the final total once the action finishes and persists
// it against whoever made the request. Kept separate from TokenUsageService (which owns the
// DB write) so the *AiService classes don't need AppDbContext at all.
public class TokenUsageAccumulator
{
    public int Total { get; private set; }

    public void Add(int tokens) => Total += tokens;
}
