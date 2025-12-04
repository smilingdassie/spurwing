// ===== DATA CONTEXT =====

// Data/RestaurantOpeningContext.cs
using Microsoft.EntityFrameworkCore;
using PosItemVerificationWeb.Models;
 

namespace PosItemVerificationWeb.Data
{
    public class RestaurantOpeningContext : DbContext
    {
        public RestaurantOpeningContext(DbContextOptions<RestaurantOpeningContext> options) : base(options)
        {
        }

        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<SystemTool> Systems { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<RestaurantOpeningTask> RestaurantOpeningTasks { get; set; }
        public DbSet<RestaurantOpeningProject> RestaurantOpeningProjects { get; set; }
        public DbSet<TaskExecution> TaskExecutions { get; set; }
        public DbSet<RestaurantEvent> RestaurantEvents { get; set; }

        // Views
       // public DbSet<VwRestaurantOpeningTask> VwRestaurantOpeningTasks { get; set; }
        public DbSet<RestaurantEventExecution> RestaurantEventExecutions { get; set; }
        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Restaurant
            modelBuilder.Entity<Restaurant>()
                .HasIndex(r => r.DCLink)
                .IsUnique();

           

            // Configure relationships
            modelBuilder.Entity<TaskExecution>()
                .HasOne(te => te.Project)
                .WithMany(p => p.TaskExecutions)
                .HasForeignKey(te => te.ProjectId);

            modelBuilder.Entity<TaskExecution>()
                .HasOne(te => te.Task)
                .WithMany()
                .HasForeignKey(te => te.TaskId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
