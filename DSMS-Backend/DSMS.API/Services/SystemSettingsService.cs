using System.Text.Json;

namespace DSMS.API.Services;

/// <summary>
/// Reads settings from appsettings.json (via IConfiguration) and allows runtime
/// overrides that are persisted to runtime-settings.json in the app root.
/// No database migration required.
/// </summary>
public class SystemSettingsService : ISystemSettingsService
{
    private readonly IConfiguration _config;
    private readonly string _overrideFile;
    private Dictionary<string, string> _overrides = new();
    private static readonly object _lock = new();

    public SystemSettingsService(IConfiguration config, IWebHostEnvironment env)
    {
        _config = config;
        _overrideFile = Path.Combine(env.ContentRootPath, "runtime-settings.json");
        LoadOverrides();
    }

    public string Get(string key, string fallback = "")
    {
        // Runtime override takes priority over appsettings.json
        if (_overrides.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
            return val;

        // Fall back to IConfiguration (appsettings.json)
        return _config[key] ?? fallback;
    }

    public void Set(string key, string value)
    {
        lock (_lock)
        {
            _overrides[key] = value;
            SaveOverrides();
        }
    }

    public Dictionary<string, string> GetAll()
    {
        // Merge appsettings.json defaults with runtime overrides
        var result = new Dictionary<string, string>();

        // Email
        result["Email:SmtpHost"]     = Get("Email:SmtpHost", "smtp.gmail.com");
        result["Email:SmtpPort"]     = Get("Email:SmtpPort", "587");
        result["Email:SenderEmail"]  = Get("Email:SenderEmail", "");
        result["Email:SenderName"]   = Get("Email:SenderName", "Arampath Driving School");
        result["Email:AppPassword"]  = Get("Email:AppPassword", ""); // returned masked

        // Twilio / WhatsApp
        result["Twilio:AccountSid"]    = Get("Twilio:AccountSid", "");
        result["Twilio:AuthToken"]     = Get("Twilio:AuthToken", "");
        result["Twilio:WhatsAppFrom"]  = Get("Twilio:WhatsAppFrom", "whatsapp:+14155238886");

        // School info
        result["SchoolInfo:Name"]    = Get("SchoolInfo:Name", "Arampath Driving School");
        result["SchoolInfo:Address"] = Get("SchoolInfo:Address", "");
        result["SchoolInfo:Phone"]   = Get("SchoolInfo:Phone", "");
        result["SchoolInfo:Email"]   = Get("SchoolInfo:Email", "");

        return result;
    }

    private void LoadOverrides()
    {
        try
        {
            if (File.Exists(_overrideFile))
            {
                var json = File.ReadAllText(_overrideFile);
                _overrides = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
        }
        catch { _overrides = new(); }
    }

    private void SaveOverrides()
    {
        try
        {
            var json = JsonSerializer.Serialize(_overrides, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_overrideFile, json);
        }
        catch { /* log silently */ }
    }
}
