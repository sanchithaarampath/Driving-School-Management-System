using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace DSMS.API.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly ISystemSettingsService _settings;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(ISystemSettingsService settings, ILogger<WhatsAppService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> SendReceiptWhatsAppAsync(string toPhone, string messageText)
    {
        try
        {
            var accountSid = _settings.Get("Twilio:AccountSid", "");
            var authToken  = _settings.Get("Twilio:AuthToken",  "");
            var from       = _settings.Get("Twilio:WhatsAppFrom", "whatsapp:+14155238886");

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) ||
                accountSid.StartsWith("YOUR_"))
            {
                _logger.LogWarning("Twilio not configured. Set credentials in System Settings.");
                return false;
            }

            // Normalise phone number — ensure it starts with country code
            var normalised = toPhone.Trim().Replace(" ", "").Replace("-", "");
            if (!normalised.StartsWith("+"))
                normalised = "+94" + normalised.TrimStart('0'); // default to Sri Lanka

            TwilioClient.Init(accountSid, authToken);

            var message = await MessageResource.CreateAsync(
                body: messageText,
                from: new Twilio.Types.PhoneNumber(from),
                to:   new Twilio.Types.PhoneNumber($"whatsapp:{normalised}")
            );

            _logger.LogInformation("WhatsApp sent to {Phone}, SID: {Sid}", normalised, message.Sid);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp to {Phone}", toPhone);
            return false;
        }
    }
}
