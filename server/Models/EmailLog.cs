namespace Server.Models;

// Audit trail of every mail EmailService has queued — written at the same moment it's handed
// to Firestore, so this reflects "we asked to send this" rather than actual SMTP delivery
// (that final state lives on the Firestore document itself, written back by the Trigger Email
// extension, which this app doesn't read back).
public class EmailLog
{
    public int Id { get; set; }
    public required string To { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
