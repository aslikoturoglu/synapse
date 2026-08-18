using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Dtos;
using Server.Models;

namespace Server.Services;

public enum ToggleRoleResult { Success, NotFound, LastAdmin }

public class UserService(AppDbContext db, EmailService email)
{
    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await db.Users.FindAsync(id);
        return user is null ? null : ToDto(user);
    }

    public async Task<List<AdminUserDto>> GetAllAdminAsync() =>
        await db.Users.OrderBy(u => u.Id).Select(u => ToAdminDto(u)).ToListAsync();

    public async Task<bool> DeactivateAsync(int id, string reason)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null)
            return false;

        user.IsDeactivated = true;
        user.DeactivationReason = reason;
        await db.SaveChangesAsync();

        await email.SendAsync(user.Email, "Your Synapse account has been deactivated",
            $"Hi {user.Name},\n\nYour account has been deactivated by an administrator.\n\nReason: {reason}\n\nIf you believe this is a mistake, please get in touch.");

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
            $"Hi {user.Name},\n\nYour account has been reactivated — you can log in again.");

        return true;
    }

    public async Task<string?> ResetPasswordAsync(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null)
            return null;

        var newPassword = GenerateRandomPassword();
        user.PasswordHash = PasswordHasher.Hash(newPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await email.SendAsync(user.Email, "Your Synapse password has been reset",
            $"Hi {user.Name},\n\nAn administrator reset your password. Your new temporary password is:\n\n{newPassword}\n\nPlease log in and change it as soon as possible.");

        return newPassword;
    }

    public async Task<ToggleRoleResult> ToggleRoleAsync(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null)
            return ToggleRoleResult.NotFound;

        // Demoting the last remaining admin would lock the whole admin panel (including the
        // ability to promote someone back) — refuse rather than allow a self-inflicted lockout.
        if (user.Role == UserRole.Admin)
        {
            var otherAdmins = await db.Users.CountAsync(u => u.Role == UserRole.Admin && u.Id != id);
            if (otherAdmins == 0)
                return ToggleRoleResult.LastAdmin;
        }

        user.Role = user.Role == UserRole.Admin ? UserRole.User : UserRole.Admin;
        await db.SaveChangesAsync();

        await email.SendAsync(user.Email, "Your Synapse account role has changed",
            $"Hi {user.Name},\n\nAn administrator changed your account role to {user.Role}.");

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

        await email.SendAsync(user.Email, "Your Synapse account has been deleted",
            $"Hi {user.Name},\n\nYour account and all associated content have been permanently deleted by an administrator.");

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

    private static AdminUserDto ToAdminDto(User user) => new()
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
        TokenUsage = 0,
    };
}
