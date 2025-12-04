using System.Security.Claims;

namespace PosItemVerificationWeb.Models
{
   

    public class AllowedUsersConfig
    {
        public string Dashboard { get; set; }
        public string POSItem { get; set; }

        public string Sensitive { get; set; }

        public HashSet<string> DashboardList { get; private set; }
        public HashSet<string> POSItemList { get; private set; }
        public HashSet<string> SensitiveList { get; private set; }
        public void Normalize()
        {
            DashboardList = Parse(Dashboard);
            POSItemList = Parse(POSItem);
            SensitiveList = Parse(Sensitive);
        }

        private HashSet<string> Parse(string csv)
        {
            return csv?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => CleanUsername(x))
                .ToHashSet()
                ?? new HashSet<string>();
        }

        private string CleanUsername(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            // strip domain if present & normalize
            var parts = input.Split('\\');
            return parts[^1].Trim().ToLower();
        }
    }
    


}
