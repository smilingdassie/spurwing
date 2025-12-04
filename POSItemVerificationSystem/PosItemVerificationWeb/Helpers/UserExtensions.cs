using System.Security.Claims;

namespace PosItemVerificationWeb.Helpers
{
    public static class UserExtensions
    {
        public static string NormalizedName(this ClaimsPrincipal user)
        {
            var name = user.Identity?.Name ?? "";
            var parts = name.Split('\\');
            return parts[^1].ToLower();
        }
    }

}
