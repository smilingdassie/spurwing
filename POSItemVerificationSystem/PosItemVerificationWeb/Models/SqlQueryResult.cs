namespace PosItemVerificationWeb.Models
{
     

    public class SqlQueryResult
    {
        public string Query { get; set; }
        public string HtmlTable { get; set; }
        public string ErrorMessage { get; set; }

        // New properties for dropdown
        public string SelectedServerDatabase { get; set; }
        public List<string> ServerDatabases { get; set; }
        public List<ScriptItem> AvailableScripts { get; set; } = new();

        public List<string> ServerConnections { get; set; } = new();


    }

    public class ScriptItem
    {
        public string ScriptName { get; set; }
        public string DisplayName { get; set; }
    }

}
