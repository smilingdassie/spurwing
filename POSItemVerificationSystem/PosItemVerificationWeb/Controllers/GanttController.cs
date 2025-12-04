// Controllers/GanttController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PosItemVerificationWeb.Data;
using PosItemVerificationWeb.Models;
using PosItemVerificationWeb.ViewModels;
using PosItemVerificationWeb.Helpers;
using Microsoft.Extensions.Options;

namespace PosItemVerificationWeb.Controllers
{
    [Authorize]
    public class GanttController : Controller
    {
        private readonly RestaurantOpeningContext _context;
        private readonly AllowedUsersConfig _allowed;
        public GanttController(RestaurantOpeningContext context, IOptions<AllowedUsersConfig> allowed)
        {
            _context = context;
            _allowed = allowed.Value;
        }

        public async Task<IActionResult> Index(int? restaurantKey)
        {
            var user = User.NormalizedName();

            if (!_allowed.DashboardList.Contains(user))
            {
                return Unauthorized();
            }
            var restaurants = await _context.Restaurants.ToListAsync();

            var viewModel = new GanttChartViewModel
            {
                Restaurants = restaurants
            };

            if (restaurantKey.HasValue)
            {
                viewModel.RestaurantKey = restaurantKey.Value;
                var restaurant = restaurants.FirstOrDefault(r => r.RestaurantKey == restaurantKey.Value);
                if (restaurant != null)
                {
                    viewModel.RestaurantName = restaurant.RestaurantName;
                    viewModel.Tasks = await GetGanttTasks(restaurantKey.Value);

                    if (viewModel.Tasks.Any())
                    {
                        viewModel.ProjectStart = viewModel.Tasks.Min(t => t.StartDate);
                        viewModel.ProjectEnd = viewModel.Tasks.Max(t => t.EndDate);
                    }
                }
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetGanttData(int restaurantKey)
        {
            var tasks = await GetGanttTasks(restaurantKey);
            return Json(tasks);
        }

        private async Task<List<GanttTask>> GetGanttTasks(int restaurantKey)
        {
            var project = await _context.RestaurantOpeningProjects
                .Include(p => p.TaskExecutions)
                .ThenInclude(te => te.Task)
                .ThenInclude(t => t.Department)
                .FirstOrDefaultAsync(p => p.RestaurantKey == restaurantKey);

            if (project == null) return new List<GanttTask>();

            var ganttTasks = new List<GanttTask>();
            var startDate = project.ProjectStartDate;

            foreach (var execution in project.TaskExecutions.OrderBy(te => te.Task.TaskId))
            {
                var task = execution.Task;
                var plannedStart = execution.PlannedStartDate ?? startDate;
                var plannedEnd = execution.PlannedEndDate ?? plannedStart.AddDays(task.EstimatedDurationDays ?? 1);

                var ganttTask = new GanttTask
                {
                    TaskId = task.TaskId,
                    TaskName = task.TaskName,
                    Department = task.Department?.DepartmentName ?? "Unknown",
                    StartDate = execution.ActualStartDate ?? plannedStart,
                    EndDate = execution.ActualEndDate ?? plannedEnd,
                    Duration = task.EstimatedDurationDays ?? 1,
                    Status = execution.Status ?? "Not Started",
                    PercentComplete = execution.PercentComplete,
                    AssignedTo = execution.AssignedTo ?? "",
                    IsCriticalPath = task.IsCriticalPath ?? false
                };

                if (task.DependsOnTaskID.HasValue)
                {
                    ganttTask.Dependencies.Add(task.DependsOnTaskID.Value);
                }

                ganttTasks.Add(ganttTask);

                // Update start date for next task if no specific dependency
                if (!task.DependsOnTaskID.HasValue)
                {
                    startDate = plannedEnd.AddDays(1);
                }
            }

            return ganttTasks;
        }
    }
}