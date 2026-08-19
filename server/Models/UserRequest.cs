namespace Server.Models;

public enum RequestStatus { Unseen, Seen, Actioned }
public enum RequestDecision { Pending, Approved, Rejected }

// A generic "ask the admin/moderator team something" ticket — free-form (Subject/Content the
// user typed themselves) except for the one reserved shape created by
// RequestService.CreateRoleChangeAsync, which sets RequestedRole. That's the only kind
// Moderators can't see or act on, since only an Admin can actually change someone's role.
public class UserRequest
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public required string Subject { get; set; }
    public required string Content { get; set; }

    // Set only for a role-change request, to the role being asked for ("User"/"Moderator") —
    // computed server-side from the requester's own current role at creation time, never taken
    // from client input, so it can't be spoofed into asking for "Admin".
    public string? RequestedRole { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Unseen;
    public RequestDecision Decision { get; set; } = RequestDecision.Pending;
    public string? Reply { get; set; }

    public int? HandledByUserId { get; set; }
    public User? HandledByUser { get; set; }
    public DateTime? HandledAt { get; set; }
}
