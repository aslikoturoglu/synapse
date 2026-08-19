using System.Text;
using Server.Dtos;

namespace Server.Services.AiFoundry;

// Reuses interactive-chat-agent-synapse (same agent as NoteChatAiService) for admin-only
// natural-language questions about platform data (users, activity, token usage). Unlike
// NoteChatAiService's per-highlight Q&A — which only needs its document context once, on the
// first turn of an exchange — this rebuilds and resends the data snapshot on every question,
// since the next question in the same conversation might depend on data that changed since it
// started (e.g. a user the admin just deleted).
public class AdminChatAiService(FoundryAgentClient client, IConfiguration configuration)
{
    private const string Instruction =
        "You are an admin assistant for the Synapse platform. Answer using ONLY the platform " +
        "data snapshot below — do not invent numbers or users that aren't listed. If the data " +
        "needed to answer isn't in the snapshot, say so plainly instead of guessing. Answer " +
        "directly; do not ask a clarifying question back unless the question is genuinely " +
        "ambiguous, in which case state your interpretation and answer anyway.";

    public async Task<(string Answer, string ResponseId)> AskAsync(
        string? existingResponseId, List<AdminUserDto> users, DashboardStatsDto stats, string question)
    {
        var agentName = configuration["AzureAiFoundry:Agents:InteractiveChat"]
            ?? throw new InvalidOperationException("AzureAiFoundry:Agents:InteractiveChat is not configured.");

        var text = $"{Instruction}\n\nPlatform data snapshot (as of {DateTime.UtcNow:u} UTC):\n{BuildSnapshot(users, stats)}\n\nQuestion: {question}";
        return await client.AskAsync(agentName, text, existingResponseId);
    }

    private static string BuildSnapshot(List<AdminUserDto> users, DashboardStatsDto stats)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Total accounts: {users.Count}");
        sb.AppendLine($"Admins: {users.Count(u => u.Role == "Admin")}, Moderators: {users.Count(u => u.Role == "Moderator")}, Regular users: {users.Count(u => u.Role == "User")}");
        sb.AppendLine($"Deactivated accounts: {users.Count(u => u.IsDeactivated)}");
        sb.AppendLine();
        sb.AppendLine("All-time activity totals:");
        sb.AppendLine($"- Posts created (shared to the public feed): {stats.PostsCreated}");
        sb.AppendLine($"- Notes created (all, shared and unshared): {stats.NotesCreated}");
        sb.AppendLine($"- Comments: {stats.CommentsCreated}");
        sb.AppendLine($"- Likes given: {stats.LikesGiven}");
        sb.AppendLine($"- Downloads: {stats.Downloads} (download tracking isn't wired up in the app yet — this is always 0, not a real signal)");
        sb.AppendLine($"- Sends: {stats.Sends} (send tracking isn't wired up in the app yet — this is always 0, not a real signal)");
        sb.AppendLine($"- AI token usage: {stats.TokenUsage} (per-user token metering isn't implemented yet — this is always 0, not a real signal)");
        sb.AppendLine();
        sb.AppendLine("Per-user detail:");
        foreach (var u in users)
        {
            var status = u.IsDeactivated ? $" — DEACTIVATED (reason: {u.DeactivationReason})" : "";
            var phone = string.IsNullOrWhiteSpace(u.Phone) ? "—" : u.Phone;
            var jobTitle = string.IsNullOrWhiteSpace(u.JobTitle) ? "—" : u.JobTitle;
            var passwordChanged = u.PasswordChangedAt is { } pc ? pc.ToString("yyyy-MM-dd") : "never";
            sb.AppendLine($"- #{u.Id} {u.Name} {u.Surname} (@{u.Username}, {u.Email}, {u.Role}){status}, phone: {phone}, job title: {jobTitle}, account created: {u.CreatedAt:yyyy-MM-dd}, password changed: {passwordChanged}");
        }

        return sb.ToString();
    }
}
