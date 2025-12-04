using PosItemVerificationWeb.Models;

namespace PosItemVerificationWeb.ViewModels
{

    public class RestaurantDashboardViewModel
    {
        public int RestaurantKey { get; set; }
        public string DCLink { get; set; }
        public string RestaurantName { get; set; }
        public string BrandName { get; set; }
        public string Status { get; set; }
        public DateTime? TargetOpeningDate { get; set; }
        public DateTime? ActualOpeningDate { get; set; }
        public int TotalEvents { get; set; }
        public int CompletedEvents { get; set; }
        public int InProgressEvents { get; set; }
        public int PendingEvents { get; set; }
        public decimal CompletionPercentage { get; set; }
        public List<EventStatusItem> EventStatuses { get; set; } = new List<EventStatusItem>();
        

        public static RestaurantDashboardViewModel FromEntity(
        Restaurant restaurant,
        IEnumerable<RestaurantEventExecution> executions,
        IEnumerable<RestaurantEvent> allEvents)
        {
            var eventStatuses = new List<EventStatusItem>();

            foreach (var evt in allEvents)
            {
                var execution = executions.FirstOrDefault(e => e.EventID == evt.ID);
                var status = execution?.Status ?? "Pending";

                var ragStatus = status switch
                {
                    "Completed" => "Green",
                    "In Progress" => "Amber",
                    _ => "Red"
                };

                eventStatuses.Add(new EventStatusItem
                {
                    EventID = evt.ID,
                    EventCode = evt.EventCode,
                    EventName = evt.EventName,
                    Status = status,
                    RagStatus = ragStatus
                });
            }

            var completed = eventStatuses.Count(e => e.Status == "Completed");
            var inProgress = eventStatuses.Count(e => e.Status == "In Progress");
            var pending = eventStatuses.Count(e => e.Status == "Pending");
            var total = eventStatuses.Count;

            return new RestaurantDashboardViewModel
            {
                RestaurantKey = restaurant.RestaurantKey,
                DCLink = restaurant.DCLink,
                RestaurantName = restaurant.RestaurantName,
                BrandName = restaurant.BrandName,
                Status = restaurant.Status,
                TargetOpeningDate = restaurant.TargetOpeningDate,
                ActualOpeningDate = restaurant.ActualOpeningDate,
                TotalEvents = total,
                CompletedEvents = completed,
                InProgressEvents = inProgress,
                PendingEvents = pending,
                CompletionPercentage = total > 0 ? Math.Round((decimal)completed / total * 100, 1) : 0,
                EventStatuses = eventStatuses
            };
        }


    }




    public class EventStatusItem
        {
            public int EventID { get; set; }
            public string EventCode { get; set; }
            public string EventName { get; set; }
            public string Status { get; set; } // Pending, In Progress, Completed
            public string RagStatus { get; set; } // Red, Amber, Green
        }
    }
