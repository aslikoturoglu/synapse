using System.Net;
using Google.Cloud.Firestore;
using Server.Data;
using Server.Models;

namespace Server.Services;

// Sends mail by writing a document to the Firestore "mail" collection; the "Trigger Email
// from Firestore" extension watches that collection and does the actual SMTP delivery. Scoped
// (not Singleton) because it now also writes to AppDbContext, which is itself Scoped.
public record EmailAttachment(string Filename, byte[] Content);

public class EmailService(FirestoreDb firestoreDb, AppDbContext db, ILogger<EmailService> logger)
{
    // replyTo/attachment are both optional and additive to the base to/subject/body send — the
    // Trigger Email extension reads replyTo and message.attachments (base64) straight off the
    // same Firestore doc alongside the fields every other caller already writes.
    public async Task SendAsync(string to, string subject, string body, string? replyTo = null, EmailAttachment? attachment = null)
    {
        var message = new Dictionary<string, object> { ["subject"] = subject, ["html"] = body };
        if (attachment is not null)
        {
            message["attachments"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["filename"] = attachment.Filename,
                    ["content"] = Convert.ToBase64String(attachment.Content),
                    ["encoding"] = "base64",
                },
            };
        }

        var mailDoc = new Dictionary<string, object> { ["to"] = to, ["message"] = message };
        if (!string.IsNullOrWhiteSpace(replyTo))
            mailDoc["replyTo"] = replyTo;

        await firestoreDb.Collection("mail").AddAsync(mailDoc);

        db.EmailLogs.Add(new EmailLog { To = to, Subject = subject, Body = body });
        await db.SaveChangesAsync();

        logger.LogInformation("[EmailService] Queued mail in Firestore. To: {To} | Subject: {Subject}", to, subject);
    }

    // Joins non-empty parts into <br><br>-separated paragraphs — callers pass conditional
    // parts (a reply, a decision, ...) as null/empty to drop them from the layout entirely
    // rather than leaving a blank paragraph behind.
    public static string Paragraphs(params string?[] parts) =>
        string.Join("<br><br>", parts.Where(p => !string.IsNullOrEmpty(p)));

    // Escapes free-text that ultimately came from a user (names, note titles, request
    // content/replies) before it's embedded in an HTML email body.
    public static string Encode(string text) => WebUtility.HtmlEncode(text);
}
