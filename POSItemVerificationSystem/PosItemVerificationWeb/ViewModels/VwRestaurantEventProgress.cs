namespace PosItemVerificationWeb.ViewModels
{

    // View model to display restaurant event progress
    public class VwRestaurantEventProgress
    {
        public int RestaurantKey { get; set; }
        public string DCLink { get; set; }
        public string RestaurantName { get; set; }
        public string BrandName { get; set; }
        public int EventID { get; set; }
        public string EventCode { get; set; }
        public string EventName { get; set; }
        public string EventDescription { get; set; }
        public string ActionName { get; set; }
        public string ActionDescription { get; set; }
        public string MasterStatus { get; set; }
        public int? ExecutionID { get; set; }
        public string ExecutionStatus { get; set; }
        public DateTime? StartedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string CompletedBy { get; set; }
        public string Notes { get; set; }

        public List<EventStatusItem> EventStatuses { get; set; } = new List<EventStatusItem>();
    }
}
