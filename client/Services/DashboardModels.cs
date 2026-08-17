namespace Client.Services;

public class DashboardStatsDto
{
    public int AccountsCreated { get; set; }
    public int PostsCreated { get; set; }
    public int NotesCreated { get; set; }
    public int CommentsCreated { get; set; }
    public int LikesGiven { get; set; }
    public int Downloads { get; set; }
    public int Sends { get; set; }
    public int TokenUsage { get; set; }
    public double AverageTokenPerPerson { get; set; }
}
