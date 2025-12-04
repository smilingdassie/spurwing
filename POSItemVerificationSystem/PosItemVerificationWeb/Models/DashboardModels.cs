namespace PosItemVerificationWeb.Models
{
    public class DashboardModels
    {
    }
    // Models/SalesForceMetric.cs
    public class SalesForceMetric
    {
        public bool Processed { get; set; }
        public string ProcessedBy { get; set; }
        public string TableName { get; set; }
        public int NumberOfRecords { get; set; }
    }

    // Models/CrmMetric.cs
    public class CrmMetric
    {
        public bool Processed { get; set; }
        public string ProcessedBy { get; set; }
        public string SourceSystem { get; set; }
        public int NumberOfRecords { get; set; }
    }

    // Models/DashboardSnapshot.cs
    public class DashboardSnapshot
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int SalesForceProcessed { get; set; }
        public int SalesForcePending { get; set; }
        public int CrmProcessed { get; set; }
        public int CrmPending { get; set; }
    }

    // Models/DashboardMetrics.cs
    public class DashboardMetrics
    {
        public DateTime Timestamp { get; set; }
        public MetricGroup SalesForce { get; set; }
        public MetricGroup Crm { get; set; }
    }
    public class DashboardMetrics2
    {
        public DateTime Timestamp { get; set; }
        public int SalesForceProcessed { get; set; }
        public int SalesForcePending { get; set; }
        public int CrmProcessed { get; set; }
        public int CrmPending { get; set; }
    }

    public class MetricGroup
    {
        public int Processed { get; set; }
        public int Pending { get; set; }
        public int Total => Processed + Pending;
        public string Trend { get; set; } // "up", "down", "stable"
    }
}
