// Controllers/TasksController.cs (SIMPLIFIED TO WORK WITHOUT COMPLEX VIEWS)
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PosItemVerificationWeb.Data;
using PosItemVerificationWeb.Models;
using PosItemVerificationWeb.Helpers;

namespace PosItemVerificationWeb.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly RestaurantOpeningContext _context;
        private readonly AllowedUsersConfig _allowed;
        public TasksController(RestaurantOpeningContext context, IOptions<AllowedUsersConfig> allowed)
        {
            _context = context;
            _allowed = allowed.Value;
        }

        public async Task<IActionResult> Index()
        {
            var user = User.NormalizedName();

            if (!_allowed.DashboardList.Contains(user))
            {
                return Unauthorized();
            }
            try
            {
                // Simple task list without complex joins for now
                var tasks = await _context.RestaurantOpeningTasks
                    .Include(t => t.Department)
                    .Include(t => t.System)
                    .Include(t => t.ResponsibleTeam)
                    .OrderBy(t => t.TaskId)
                    .ToListAsync();

                return View(tasks);
            }
            catch (Exception ex)
            {
                // Return empty list if database issues
                return View(new List<RestaurantOpeningTask>());
            }
        }
    }
}
