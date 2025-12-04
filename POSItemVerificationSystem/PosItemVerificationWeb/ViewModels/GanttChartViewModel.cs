using PosItemVerificationWeb.Models;

namespace PosItemVerificationWeb.ViewModels
{
     
        public class GanttChartViewModel
        {
            public int RestaurantKey { get; set; }
            public string RestaurantName { get; set; }
            public List<GanttTask> Tasks { get; set; } = new List<GanttTask>();
            public List<Restaurant> Restaurants { get; set; } = new List<Restaurant>();
            public DateTime ProjectStart { get; set; }
            public DateTime ProjectEnd { get; set; }
        }

        public class GanttTask
        {
            public int TaskId { get; set; }
            public string TaskName { get; set; }
            public string Department { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int Duration { get; set; }
            public string Status { get; set; }
            public int PercentComplete { get; set; }
            public string AssignedTo { get; set; }
            public List<int> Dependencies { get; set; } = new List<int>();
            public bool IsCriticalPath { get; set; }
        }
    }

