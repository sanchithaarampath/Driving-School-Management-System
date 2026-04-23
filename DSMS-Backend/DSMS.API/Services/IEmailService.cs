namespace DSMS.API.Services;

public interface IEmailService
{
    Task<bool> SendReceiptEmailAsync(string toEmail, string studentName, string receiptHtml, string billNumber);
}
