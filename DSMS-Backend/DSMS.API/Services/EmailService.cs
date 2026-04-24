using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DSMS.API.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendReceiptEmailAsync(string toEmail, string studentName, string receiptHtml, string billNumber)
    {
        try
        {
            var emailConfig = _config.GetSection("Email");
            var smtpHost = emailConfig["SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(emailConfig["SmtpPort"] ?? "587");
            var senderEmail = emailConfig["SenderEmail"] ?? "";
            var senderName = emailConfig["SenderName"] ?? "Arampath Driving School";
            var appPassword = emailConfig["AppPassword"] ?? "";

            if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(appPassword))
            {
                _logger.LogWarning("Email configuration is incomplete. Skipping email send.");
                return false;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(studentName, toEmail));
            message.Subject = $"Payment Receipt — {billNumber} | {senderName}";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = receiptHtml,
                TextBody = $"Dear {studentName},\n\nPlease find your payment receipt for {billNumber} attached.\n\nThank you for choosing {senderName}."
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(senderEmail, appPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Receipt email sent to {Email} for bill {BillNumber}", toEmail, billNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send receipt email to {Email}", toEmail);
            return false;
        }
    }
}
