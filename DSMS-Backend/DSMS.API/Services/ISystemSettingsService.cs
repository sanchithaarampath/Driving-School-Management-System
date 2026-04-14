namespace DSMS.API.Services;

public interface ISystemSettingsService
{
    string Get(string key, string fallback = "");
    void Set(string key, string value);
    Dictionary<string, string> GetAll();
}
