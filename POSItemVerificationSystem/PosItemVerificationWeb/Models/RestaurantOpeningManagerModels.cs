using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PosItemVerificationWeb.Models
{
    

    
        public class Restaurant
        {
            [Key]
            public int RestaurantKey { get; set; }

            [Required]
            [StringLength(50)]
            public string DCLink { get; set; }

            [Required]
            [StringLength(200)]
            public string RestaurantName { get; set; }

            [StringLength(100)]
            public string BrandName { get; set; }

            [StringLength(200)]
            public string Address { get; set; }

            [StringLength(100)]
            public string City { get; set; }

            [StringLength(50)]
            public string Province { get; set; }

            [StringLength(20)]
            public string PostalCode { get; set; }

            public DateTime? TargetOpeningDate { get; set; }

            public DateTime? ActualOpeningDate { get; set; }

            [StringLength(50)]
            public string Status { get; set; } = "Planning";

            public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [NotMapped]
        public virtual ICollection<RestaurantOpeningProject>? OpeningProjects { get; set; }
        }



    public class RestaurantOpeningProject
    {
        [Key]
        public int ProjectId { get; set; }

        [Required]
        public int RestaurantKey { get; set; }

        [Required]
        [StringLength(200)]
        public string ProjectName { get; set; }

        public DateTime ProjectStartDate { get; set; }

        public DateTime? ProjectEndDate { get; set; }

        public DateTime? ActualEndDate { get; set; }

        [StringLength(50)]
        public string ProjectStatus { get; set; } = "Not Started";

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("RestaurantKey")]
        public virtual Restaurant Restaurant { get; set; }

        public virtual ICollection<TaskExecution> TaskExecutions { get; set; }
    }
    public class TaskExecution
    {
        [Key]
        public int ExecutionId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int TaskId { get; set; }

        public DateTime? PlannedStartDate { get; set; }

        public DateTime? PlannedEndDate { get; set; }

        public DateTime? ActualStartDate { get; set; }

        public DateTime? ActualEndDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Not Started";

        [StringLength(500)]
        public string Notes { get; set; }

        [StringLength(100)]
        public string AssignedTo { get; set; }

        public int PercentComplete { get; set; } = 0;

        // Navigation properties
        [ForeignKey("ProjectId")]
        public virtual RestaurantOpeningProject Project { get; set; }

        [ForeignKey("TaskId")]
        public virtual RestaurantOpeningTask Task { get; set; }
    }



        public class Department
        {
            [Key]
            public int DepartmentID { get; set; }

            [Required]
            [StringLength(50)]
            public string DepartmentName { get; set; } = string.Empty;

            [StringLength(255)]
            public string? Description { get; set; }

            // Navigation properties
            public virtual ICollection<RestaurantOpeningTask> Tasks { get; set; } = new List<RestaurantOpeningTask>();
            public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
        }

        public class SystemTool
        {
            [Key]
            public int SystemID { get; set; }

            [Required]
            [StringLength(50)]
            public string SystemName { get; set; } = string.Empty;

            [StringLength(255)]
            public string? Description { get; set; }

            // Navigation properties
            public virtual ICollection<RestaurantOpeningTask> Tasks { get; set; } = new List<RestaurantOpeningTask>();
        }
   


        public class Team
        {
            [Key]
            public int TeamID { get; set; }

            [Required]
            [StringLength(100)]
            public string TeamName { get; set; } = string.Empty;

            public int? DepartmentID { get; set; }

            // Navigation properties
            [ForeignKey("DepartmentID")]
            public virtual Department? Department { get; set; }

            public virtual ICollection<RestaurantOpeningTask> Tasks { get; set; } = new List<RestaurantOpeningTask>();
        }

    public class RestaurantOpeningTask
    {
        [Key]
        public int TaskId { get; set; }

        [Required]
        [StringLength(200)]
        public string TaskName { get; set; } = string.Empty;

        public string? TaskDescription { get; set; }

        [Required]
        public int DepartmentID { get; set; }

        public int? SystemID { get; set; }

        public int? ResponsibleTeamID { get; set; }

        public int? DependsOnTaskID { get; set; }

        [StringLength(500)]
        public string? StatusOptions { get; set; }

        public int? EstimatedDurationDays { get; set; }

        public bool? IsCriticalPath { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("DepartmentID")]
        public virtual Department Department { get; set; } = null!;

        [ForeignKey("SystemID")]
        public virtual SystemTool? System { get; set; }

        [ForeignKey("ResponsibleTeamID")]
        public virtual Team? ResponsibleTeam { get; set; }

        [ForeignKey("DependsOnTaskID")]
        public virtual RestaurantOpeningTask? DependsOnTask { get; set; }

        public virtual ICollection<RestaurantOpeningTask> DependentTasks { get; set; } = new List<RestaurantOpeningTask>();
    }
    public class VwRestaurantOpeningTask
        {
            public int TaskID { get; set; }
            public string TaskName { get; set; } = string.Empty;
            public string? TaskDescription { get; set; }
            public string DepartmentName { get; set; } = string.Empty;
            public string? SystemName { get; set; }
            public string? TeamName { get; set; }
            public string? DependsOnTask { get; set; }
            public string? StatusOptions { get; set; }
            public int? EstimatedDurationDays { get; set; }
            public bool? IsCriticalPath { get; set; }
            public DateTime CreatedDate { get; set; }
            public DateTime ModifiedDate { get; set; }
            public int? RestaurantKey { get; set; }
            public string? DCLink { get; set; }
            public string? RestaurantName { get; set; }
            public string? BrandName { get; set; }
            public int? ExecutionId { get; set; }
            public int? ProjectId { get; set; }
            public DateTime? PlannedStartDate { get; set; }
            public DateTime? PlannedEndDate { get; set; }
            public DateTime? ActualStartDate { get; set; }
            public DateTime? ActualEndDate { get; set; }
            public string? ExecutionStatus { get; set; }
            public int PercentComplete { get; set; }
            public string? AssignedTo { get; set; }
        }
   

}


