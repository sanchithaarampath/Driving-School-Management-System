using System.Security.Claims;

namespace DSMS.API.Helpers;

public static class ClaimsHelper
{
    public static int? GetBranchId(ClaimsPrincipal user)
    {
        var val = user.FindFirstValue("BranchId");
        return string.IsNullOrEmpty(val) ? null : int.Parse(val);
    }

    public static string GetRole(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Role) ?? "";

    public static bool IsCompanyAdmin(ClaimsPrincipal user)
        => GetRole(user) == "Company Admin" || GetRole(user) == "Admin";

    public static bool IsBranchAdmin(ClaimsPrincipal user)
        => GetRole(user) == "Branch Admin";

    public static bool IsStaff(ClaimsPrincipal user)
        => GetRole(user) == "Staff";

    public static bool IsInstructor(ClaimsPrincipal user)
        => GetRole(user) == "Instructor";
}
