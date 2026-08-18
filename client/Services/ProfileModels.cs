namespace Client.Services;

public class ProfileSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Surname { get; set; } = "";
    public string Username { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int PostCount { get; set; }
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public bool FollowedByMe { get; set; }

    public string FullName => $"{Name} {Surname}";
}
