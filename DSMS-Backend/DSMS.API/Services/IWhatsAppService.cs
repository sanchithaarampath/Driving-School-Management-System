namespace DSMS.API.Services;

public interface IWhatsAppService
{
    Task<bool> SendReceiptWhatsAppAsync(string toPhone, string messageText);
}
