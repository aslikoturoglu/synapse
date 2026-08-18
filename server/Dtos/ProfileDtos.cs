namespace Server.Dtos;

public class ProfileSummaryDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required string Surname { get; set; }
    public required string Username { get; set; }
    public required string JobTitle { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required int PostCount { get; set; }
    public required int FollowerCount { get; set; }
    public required int FollowingCount { get; set; }
    public required bool FollowedByMe { get; set; }
}
