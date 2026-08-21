using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Dtos;
using Server.Models;

namespace Server.Services;

public enum RespondResult { Success, NotFound, Forbidden, InvalidDecision }

public class RequestService(AppDbContext db, UserService userService, EmailService email)
{
    // Moderators see every request except role-change ones (only an Admin can act on those)
    // and their own (a moderator reviewing their own ticket is a conflict of interest — they
    // can still track it themselves from My Requests, just not action it here).
    public async Task<List<UserRequestDto>> GetAllAsync(bool isModerator, int currentUserId)
    {
        var query = db.UserRequests.Include(r => r.User).AsQueryable();
        if (isModerator)
            query = query.Where(r => r.RequestedRole == null && r.UserId != currentUserId);

        return await query.OrderByDescending(r => r.CreatedAt)
            .Select(r => ToDto(r))
            .ToListAsync();
    }

    public async Task<List<UserRequestDto>> GetMineAsync(int userId) =>
        await db.UserRequests.Include(r => r.User).Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => ToDto(r))
            .ToListAsync();

    public async Task<UserRequestDto> CreateAsync(int userId, string subject, string content)
    {
        var request = new UserRequest { UserId = userId, Subject = subject, Content = content };
        db.UserRequests.Add(request);
        await db.SaveChangesAsync();

        var loaded = await db.UserRequests.Include(r => r.User).FirstAsync(r => r.Id == request.Id);
        await NotifyStaffOfNewRequestAsync(loaded);
        return ToDto(loaded);
    }

    // Role direction is derived from the caller's own current role, never from client input —
    // an Admin has nothing to request (returns null), and re-requesting while one's already
    // pending just returns that same request instead of piling up duplicates.
    public async Task<UserRequestDto?> CreateRoleChangeAsync(int userId)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null || user.Role == UserRole.Admin)
            return null;

        var existing = await db.UserRequests
            .Where(r => r.UserId == userId && r.RequestedRole != null && r.Decision == RequestDecision.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();
        if (existing is not null)
            return ToDto(await db.UserRequests.Include(r => r.User).FirstAsync(r => r.Id == existing.Id));

        var targetRole = user.Role == UserRole.Moderator ? "User" : "Moderator";
        var request = new UserRequest
        {
            UserId = userId,
            Subject = "Role Change",
            Content = $"Requesting a role change from {user.Role} to {targetRole}.",
            RequestedRole = targetRole,
        };
        db.UserRequests.Add(request);
        await db.SaveChangesAsync();

        var loaded = await db.UserRequests.Include(r => r.User).FirstAsync(r => r.Id == request.Id);
        await NotifyStaffOfNewRequestAsync(loaded);
        return ToDto(loaded);
    }

    public async Task<bool> MarkSeenAsync(int id, bool isModerator)
    {
        var request = await db.UserRequests.FindAsync(id);
        if (request is null || (isModerator && request.RequestedRole is not null))
            return false;

        if (request.Status == RequestStatus.Unseen)
        {
            request.Status = RequestStatus.Seen;
            await db.SaveChangesAsync();
        }

        return true;
    }

    // A reply and/or a decision — either (or both) counts as "did something", moving the
    // request to Actioned. Approving a role-change request also actually performs the role
    // change (reusing UserService's own User<->Moderator toggle and its Admin protection).
    public async Task<RespondResult> RespondAsync(int id, int handledByUserId, bool isModerator, string? reply, string? decision)
    {
        var request = await db.UserRequests.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == id);
        if (request is null)
            return RespondResult.NotFound;
        if (isModerator && request.RequestedRole is not null)
            return RespondResult.Forbidden;

        var didSomething = false;

        if (!string.IsNullOrWhiteSpace(reply))
        {
            request.Reply = reply.Trim();
            didSomething = true;
        }

        if (decision is not null)
        {
            if (!Enum.TryParse<RequestDecision>(decision, out var parsedDecision) || parsedDecision == RequestDecision.Pending)
                return RespondResult.InvalidDecision;

            request.Decision = parsedDecision;
            didSomething = true;

            if (request.RequestedRole is not null && parsedDecision == RequestDecision.Approved)
                await userService.ToggleRoleAsync(request.UserId);
        }

        if (!didSomething)
            return RespondResult.Success;

        request.Status = RequestStatus.Actioned;
        request.HandledByUserId = handledByUserId;
        request.HandledAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await email.SendAsync(request.User.Email, $"Your request has been answered: {request.Subject}",
            EmailService.Paragraphs(
                $"Hi {EmailService.Encode(request.User.Name)},",
                $"Your request \"<strong>{EmailService.Encode(request.Subject)}</strong>\" has been reviewed.",
                $"<strong>Your request:</strong><br>{EmailService.Encode(request.Content)}",
                !string.IsNullOrWhiteSpace(request.Reply) ? $"<strong>Reply:</strong><br>{EmailService.Encode(request.Reply)}" : null,
                request.Decision != RequestDecision.Pending ? $"<strong>Decision:</strong> {request.Decision}" : null));

        return RespondResult.Success;
    }

    // New request in ("talep") → staff (Admin, plus Moderator unless it's a role-change
    // request, since only an Admin can act on those — see RespondAsync's Forbidden check
    // and the class-level comment on GetAllAsync). Never the requester themselves.
    private async Task NotifyStaffOfNewRequestAsync(UserRequest request)
    {
        var staffEmails = await db.Users
            .Where(u => u.Id != request.UserId && (request.RequestedRole != null
                ? u.Role == UserRole.Admin
                : (u.Role == UserRole.Admin || u.Role == UserRole.Moderator)))
            .Select(u => u.Email)
            .ToListAsync();

        var body = EmailService.Paragraphs(
            $"<strong>{EmailService.Encode(request.User.Name)} {EmailService.Encode(request.User.Surname)}</strong> (@{EmailService.Encode(request.User.Username)}) submitted a new request.",
            $"<strong>Subject:</strong> {EmailService.Encode(request.Subject)}",
            EmailService.Encode(request.Content));
        foreach (var staffEmail in staffEmails)
            await email.SendAsync(staffEmail, $"New request: {request.Subject}", body);
    }

    private static UserRequestDto ToDto(UserRequest r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        RequesterName = r.User.Name + " " + r.User.Surname,
        UserEmail = r.User.Email,
        Username = r.User.Username,
        CreatedAt = r.CreatedAt,
        Subject = r.Subject,
        Content = r.Content,
        RequestedRole = r.RequestedRole,
        Status = r.Status.ToString(),
        Decision = r.Decision.ToString(),
        Reply = r.Reply,
        HandledAt = r.HandledAt,
    };
}
