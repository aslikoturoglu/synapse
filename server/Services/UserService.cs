using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Dtos;
using Server.Models;

namespace Server.Services;

public enum ToggleRoleResult { Success, NotFound, CannotChangeAdmin }

public class UserService(AppDbContext db, EmailService email, TokenUsageService tokenUsageService)
{
    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await db.Users.FindAsync(id);
        return user is null ? null : ToDto(user);
    }

    public async Task<List<AdminUserDto>> GetAllAdminAsync()
    {
        var users = await db.Users.OrderBy(u => u.Id).ToListAsync();
        var tokenTotals = await tokenUsageService.GetLifetimeTotalsByUserAsync();
        return users.Select(u => ToAdminDto(u, tokenTotals.GetValueOrDefault(u.Id))).ToList();
    }

    public async Task<bool> DeactivateAsync(int id, string reason)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null)
            return false;

        user.IsDeactivated = true;
        user.DeactivationReason = reason;
        await db.SaveChangesAsync();

        await email.SendAsync(user.Email, "Your Synapse account has been deactivated",
            EmailService.Paragraphs(
                $"Hi {EmailService.Encode(user.Name)},",
                "Your account has been deactivated by an administrator.",
                $"<strong>Reason:</strong> {EmailService.Encode(reason)}",
                "If you believe this is a mistake, please get in touch."));

        return true;
    }

    public async Task<bool> ReactivateAsync(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null)
            return false;

        user.IsDeactivated = false;
        user.DeactivationReason = null;
        await db.SaveChangesAsync();

        await email.SendAsync(user.Email, "Your Synapse account has been reactivated",
            EmailService.Paragraphs(
                $"Hi {EmailService.Encode(user.Name)},",
                "Your account has been reactivated — you can log in again."));

        return true;
    }

    public async Task<string?> ResetPasswordAsync(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null)
            return null;

        var newPassword = await ReplacePasswordAsync(user);

        await email.SendAsync(user.Email, "Your Synapse password has been reset",
            EmailService.Paragraphs(
                $"Hi {EmailService.Encode(user.Name)},",
                "An administrator reset your password. Your new temporary password is:",
                $"<strong>{newPassword}</strong>",
                "Please log in and change it as soon as possible."));

        return newPassword;
    }

    // Self-service counterpart to ResetPasswordAsync (same "new random password, mailed
    // out" mechanism, just triggered by the user themselves instead of an admin) — the
    // caller (AuthController) always returns the same generic response regardless of what
    // this does, so it deliberately returns nothing an API response could leak.
    public async Task ForgotPasswordAsync(string emailAddress)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == emailAddress);
        if (user is null)
            return;

        var newPassword = await ReplacePasswordAsync(user);

        await email.SendAsync(user.Email, "Your Synapse password has been reset",
            EmailService.Paragraphs(
                $"Hi {EmailService.Encode(user.Name)},",
                "We received a request to reset your password. Your new temporary password is:",
                $"<strong>{newPassword}</strong>",
                "If you didn't request this, please contact an administrator.",
                "Please log in and change it as soon as possible."));
    }

    private async Task<string> ReplacePasswordAsync(User user)
    {
        var newPassword = GenerateRandomPassword();
        user.PasswordHash = PasswordHasher.Hash(newPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return newPassword;
    }

    // This is a Moderator grant/revoke toggle (User &lt;-&gt; Moderator), not a general role editor —
    // it only ever operates on that pair. An Admin account is protected outright: this endpoint
    // is Admin-only to call (UsersController), and even an Admin can't demote a fellow Admin
    // (or, self-evidently, themselves) through it, since "our real admin(s)" is meant to stay a
    // deliberate, out-of-band decision rather than a click away.
    public async Task<ToggleRoleResult> ToggleRoleAsync(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null)
            return ToggleRoleResult.NotFound;

        if (user.Role == UserRole.Admin)
            return ToggleRoleResult.CannotChangeAdmin;

        user.Role = user.Role == UserRole.Moderator ? UserRole.User : UserRole.Moderator;
        await db.SaveChangesAsync();

        await email.SendAsync(user.Email, "Your Synapse account role has changed",
            EmailService.Paragraphs(
                $"Hi {EmailService.Encode(user.Name)},",
                $"An administrator changed your account role to <strong>{user.Role}</strong>."));

        return ToggleRoleResult.Success;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null)
            return false;

        // These relations are Restrict (not Cascade) in AppDbContext — a comment/highlight this
        // user made on someone else's post, or a follow row on either side, won't cascade away
        // just because the post/other user isn't being deleted, so they have to be cleared
        // explicitly before the user row can go.
        await db.Comments.Where(c => c.AuthorId == id).ExecuteDeleteAsync();
        await db.NoteHighlights.Where(h => h.UserId == id).ExecuteDeleteAsync();
        await db.Follows.Where(f => f.FollowerId == id || f.FollowingId == id).ExecuteDeleteAsync();
        await db.UserRequests.Where(r => r.UserId == id || r.HandledByUserId == id).ExecuteDeleteAsync();

        await email.SendAsync(user.Email, "Your Synapse account has been deleted",
            EmailService.Paragraphs(
                $"Hi {EmailService.Encode(user.Name)},",
                "Your account and all associated content have been permanently deleted by an administrator."));

        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return true;
    }

    private static string GenerateRandomPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@#$%";
        var all = upper + lower + digits + symbols;

        var chars = new char[12];
        chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        chars[3] = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];
        for (var i = 4; i < chars.Length; i++)
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    private static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Surname = user.Surname,
        Username = user.Username,
        Email = user.Email,
        Phone = user.Phone,
        JobTitle = user.JobTitle,
        Role = user.Role.ToString(),
        IsDeactivated = user.IsDeactivated,
        DeactivationReason = user.DeactivationReason,
    };

    private static AdminUserDto ToAdminDto(User user, int tokenUsage) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Surname = user.Surname,
        Username = user.Username,
        Email = user.Email,
        Phone = user.Phone,
        JobTitle = user.JobTitle,
        Role = user.Role.ToString(),
        CreatedAt = user.CreatedAt,
        PasswordChangedAt = user.PasswordChangedAt,
        IsDeactivated = user.IsDeactivated,
        DeactivationReason = user.DeactivationReason,
        TokenUsage = tokenUsage,
    };
}
