namespace Server.Services;

// No SMTP/SendGrid provider is wired up yet — this logs what would have been sent so every
// caller (deactivate/reactivate/reset-password/delete) is already fully wired end-to-end.
// Swapping in a real provider later only means rewriting SendAsync's body here.
public class EmailService(ILogger<EmailService> logger)
{
    public Task SendAsync(string to, string subject, string body)
    {
        logger.LogInformation("[EmailService] (not actually sent — no provider configured) To: {To} | Subject: {Subject}\n{Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
