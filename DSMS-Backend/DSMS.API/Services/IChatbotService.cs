namespace DSMS.API.Services;

public interface IChatbotService
{
    Task<string> ProcessMessageAsync(string message, string role, int? branchId, string userName);
}
