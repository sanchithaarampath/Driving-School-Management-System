using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DSMS.API.Services;

namespace DSMS.API.Controllers;

[ApiController]
[Route("api/system-settings")]
[Authorize]
public class SystemSettingsController : ControllerBase
{
    private readonly ISystemSettingsService _settings;
    private readonly IEmailService _emailService;
    private readonly IWhatsAppService _whatsAppService;

    public SystemSettingsController(
        ISystemSettingsService settings,
        IEmailService emailService,
        IWhatsAppService whatsAppService)
    {
        _settings = settings;
        _emailService = emailService;
        _whatsAppService = whatsAppService;
    }

    // GET — returns current settings (secrets masked)
    [HttpGet]
    public IActionResult Get()
    {
        var all = _settings.GetAll();

        // Mask secrets before sending to frontend
        string Mask(string val) => string.IsNullOrWhiteSpace(val) || val.StartsWith("YOUR_")
            ? ""
            : val.Length > 6 ? val[..3] + new string('*', val.Length - 6) + val[^3..] : "***";

        return Ok(new
        {
            email = new
            {
                smtpHost    = all["Email:SmtpHost"],
                smtpPort    = all["Email:SmtpPort"],
                senderEmail = all["Email:SenderEmail"],
                senderName  = all["Email:SenderName"],
                appPassword = Mask(all["Email:AppPassword"]),
                configured  = !string.IsNullOrWhiteSpace(all["Email:SenderEmail"]) &&
                              !string.IsNullOrWhiteSpace(all["Email:AppPassword"]) &&
                              !all["Email:AppPassword"].Contains("YOUR_")
            },
            twilio = new
            {
                accountSid   = Mask(all["Twilio:AccountSid"]),
                authToken    = Mask(all["Twilio:AuthToken"]),
                whatsAppFrom = all["Twilio:WhatsAppFrom"],
                configured   = !string.IsNullOrWhiteSpace(all["Twilio:AccountSid"]) &&
                               !all["Twilio:AccountSid"].StartsWith("YOUR_")
            },
            school = new
            {
                name    = all["SchoolInfo:Name"],
                address = all["SchoolInfo:Address"],
                phone   = all["SchoolInfo:Phone"],
                email   = all["SchoolInfo:Email"]
            }
        });
    }

    // PUT — update settings (Company Admin only)
    [HttpPut]
    public IActionResult Update([FromBody] UpdateSettingsDto dto)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
        if (role != "Company Admin" && role != "Admin")
            return Forbid();

        if (dto.Email != null)
        {
            if (!string.IsNullOrWhiteSpace(dto.Email.SmtpHost))
                _settings.Set("Email:SmtpHost",    dto.Email.SmtpHost);
            if (!string.IsNullOrWhiteSpace(dto.Email.SmtpPort))
                _settings.Set("Email:SmtpPort",    dto.Email.SmtpPort);
            if (!string.IsNullOrWhiteSpace(dto.Email.SenderEmail))
                _settings.Set("Email:SenderEmail", dto.Email.SenderEmail);
            if (!string.IsNullOrWhiteSpace(dto.Email.SenderName))
                _settings.Set("Email:SenderName",  dto.Email.SenderName);
            if (!string.IsNullOrWhiteSpace(dto.Email.AppPassword))
                _settings.Set("Email:AppPassword", dto.Email.AppPassword);
        }

        if (dto.Twilio != null)
        {
            if (!string.IsNullOrWhiteSpace(dto.Twilio.AccountSid))
                _settings.Set("Twilio:AccountSid",   dto.Twilio.AccountSid);
            if (!string.IsNullOrWhiteSpace(dto.Twilio.AuthToken))
                _settings.Set("Twilio:AuthToken",    dto.Twilio.AuthToken);
            if (!string.IsNullOrWhiteSpace(dto.Twilio.WhatsAppFrom))
                _settings.Set("Twilio:WhatsAppFrom", dto.Twilio.WhatsAppFrom);
        }

        if (dto.School != null)
        {
            if (!string.IsNullOrWhiteSpace(dto.School.Name))
                _settings.Set("SchoolInfo:Name",    dto.School.Name);
            if (!string.IsNullOrWhiteSpace(dto.School.Address))
                _settings.Set("SchoolInfo:Address", dto.School.Address);
            if (!string.IsNullOrWhiteSpace(dto.School.Phone))
                _settings.Set("SchoolInfo:Phone",   dto.School.Phone);
            if (!string.IsNullOrWhiteSpace(dto.School.Email))
                _settings.Set("SchoolInfo:Email",   dto.School.Email);
        }

        return Ok(new { message = "Settings saved successfully." });
    }

    // POST — test email
    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail([FromBody] TestEmailDto dto)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
        if (role != "Company Admin" && role != "Admin") return Forbid();

        var testHtml = "<h2>Test Email from DSMS</h2><p>Email configuration is working correctly!</p>";
        var ok = await _emailService.SendReceiptEmailAsync(dto.ToEmail, "Admin", testHtml, "TEST-001");
        return ok
            ? Ok(new { message = $"Test email sent to {dto.ToEmail}" })
            : BadRequest(new { message = "Failed — check Email credentials in settings." });
    }

    // POST — test WhatsApp
    [HttpPost("test-whatsapp")]
    public async Task<IActionResult> TestWhatsApp([FromBody] TestWhatsAppDto dto)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
        if (role != "Company Admin" && role != "Admin") return Forbid();

        var msg = "🚗 *DSMS Test Message*\nWhatsApp configuration is working correctly!";
        var ok = await _whatsAppService.SendReceiptWhatsAppAsync(dto.ToPhone, msg);
        return ok
            ? Ok(new { message = $"Test WhatsApp sent to {dto.ToPhone}" })
            : BadRequest(new { message = "Failed — check Twilio credentials in settings." });
    }
}

// ── DTOs ─────────────────────────────────────────────────────────────────────
public class UpdateSettingsDto
{
    public EmailSettingsDto?  Email  { get; set; }
    public TwilioSettingsDto? Twilio { get; set; }
    public SchoolSettingsDto? School { get; set; }
}

public class EmailSettingsDto
{
    public string? SmtpHost    { get; set; }
    public string? SmtpPort    { get; set; }
    public string? SenderEmail { get; set; }
    public string? SenderName  { get; set; }
    public string? AppPassword { get; set; }
}

public class TwilioSettingsDto
{
    public string? AccountSid   { get; set; }
    public string? AuthToken    { get; set; }
    public string? WhatsAppFrom { get; set; }
}

public class SchoolSettingsDto
{
    public string? Name    { get; set; }
    public string? Address { get; set; }
    public string? Phone   { get; set; }
    public string? Email   { get; set; }
}

public class TestEmailDto    { public string ToEmail { get; set; } = ""; }
public class TestWhatsAppDto { public string ToPhone { get; set; } = ""; }
