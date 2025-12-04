 
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace PosItemVerificationWeb.Models
    {
        // This tracks each restaurant's progress through the master events
        public class RestaurantEventExecution
        {
            [Key]
            public int ExecutionID { get; set; }

            [Required]
            public int RestaurantKey { get; set; }

            [Required]
            public int EventID { get; set; }

            [StringLength(50)]
            public string Status { get; set; } = "Pending"; // Pending, In Progress, Completed, Skipped

            public DateTime? StartedDate { get; set; }

            public DateTime? CompletedDate { get; set; }

            [StringLength(1000)]
            public string Notes { get; set; }

            [StringLength(100)]
            public string CompletedBy { get; set; }

            public DateTime CreatedDate { get; set; } = DateTime.Now;

            public DateTime ModifiedDate { get; set; } = DateTime.Now;

            // Navigation properties
            [ForeignKey("RestaurantKey")]
            public virtual Restaurant Restaurant { get; set; }

            [ForeignKey("EventID")]
            public virtual RestaurantEvent Event { get; set; }
        }

        
    }
