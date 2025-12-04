namespace ResendEmailApi.Models
{
    public class EmailResponse
    {
        public bool Success { get; set; }
        public string? MessageId { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }

    public class BulkEmailResponse
    {
        public bool Success { get; set; }
        public int TotalSent { get; set; }
        public int TotalFailed { get; set; }
        public List<EmailResult> Results { get; set; } = new();
    }

    public class EmailResult
    {
        public string Recipient { get; set; }
        public bool Success { get; set; }
        public string? MessageId { get; set; }
        public string? Error { get; set; }
    }

    public class EmailStatsResponse
    {
        public string? Id { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastEvent { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        public string? Subject { get; set; }
        public List<EmailEvent>? Events { get; set; }
    }

    public class EmailEvent
    {
        public string Type { get; set; }  // delivered, opened, clicked, bounced, etc.
        public DateTime Timestamp { get; set; }
        public string? Reason { get; set; }  // For bounces/failures
    }

    public class EmailListResponse
    {
        public List<EmailStatsResponse> Emails { get; set; } = new();
        public int TotalCount { get; set; }
    }
}