using System.Net;
using Google.Cloud.Firestore;

namespace Server.Services;

// Sends mail by writing a document to the Firestore "mail" collection; the "Trigger Email
// from Firestore" extension watches that collection and does the actual SMTP delivery.
public class EmailService(FirestoreDb firestoreDb, ILogger<EmailService> logger)
{
    public async Task SendAsync(string to, string subject, string body)
    {
        await firestoreDb.Collection("mail").AddAsync(new
        {
            to,
            message = new { subject, html = body },
        });

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
