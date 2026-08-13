using Microsoft.EntityFrameworkCore;
using Server.Models;
using Server.Services;

namespace Server.Data;

// Ensures the one built-in admin account exists. Runs once at startup (idempotent — skips
// once the row is there), the same way a first-run migration would, since there's no
// separate admin-provisioning flow in this app.
public static class AdminSeeder
{
    private const string Email = "admin@synapse";
    private const string Username = "admin";
    private const string Password = "Admin123";

    public static async Task EnsureAdminAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync(u => u.Username == Username || u.Email == Email))
            return;

        var admin = new User
        {
            Name = "Admin",
            Surname = "Synapse",
            Email = Email,
            Username = Username,
            PasswordHash = PasswordHasher.Hash(Password),
            Role = UserRole.Admin,
        };
        admin.Settings = new UserSettings { User = admin };

        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}
