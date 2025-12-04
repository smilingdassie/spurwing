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
    public class RestaurantsController : Controller
    {
        private readonly RestaurantOpeningContext _context;
        private readonly AllowedUsersConfig _allowed;
        public RestaurantsController(RestaurantOpeningContext context, IOptions<AllowedUsersConfig> allowed)
        {
            _context = context;
            _allowed = allowed.Value;
        }

        // GET: Restaurants
        public async Task<IActionResult> Index()
        {
            var user = User.NormalizedName();

            if (!_allowed.DashboardList.Contains(user))
            {
                return Unauthorized();
            }
            return View(await _context.Restaurants.ToListAsync());
        }

        // GET: Restaurants/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var restaurant = await _context.Restaurants
                .Include(r => r.OpeningProjects)
                .ThenInclude(p => p.TaskExecutions)
                .ThenInclude(te => te.Task)
                .FirstOrDefaultAsync(m => m.RestaurantKey == id);

            if (restaurant == null) return NotFound();

            return View(restaurant);
        }

        // GET: Restaurants/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DCLink,RestaurantName,BrandName,Address,City,Province,PostalCode,TargetOpeningDate,Status")] Restaurant restaurant)
        {
            // Make sure OpeningProjects is initialized as empty
            restaurant.OpeningProjects = new List<RestaurantOpeningProject>();

            if (ModelState.IsValid)
            {
                _context.Add(restaurant);
                await _context.SaveChangesAsync();

                // Auto-create opening project
                await CreateOpeningProject(restaurant.RestaurantKey);

                return RedirectToAction(nameof(Index));
            }
            return View(restaurant);
        }

        // GET: Restaurants/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null) return NotFound();

            return View(restaurant);
        }

        // POST: Restaurants/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RestaurantKey,DCLink,RestaurantName,BrandName,Address,City,Province,PostalCode,TargetOpeningDate,ActualOpeningDate,Status,CreatedDate")] Restaurant restaurant)
        {
            if (id != restaurant.RestaurantKey) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(restaurant);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RestaurantExists(restaurant.RestaurantKey))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(restaurant);
        }

        // GET: Restaurants/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(m => m.RestaurantKey == id);
            if (restaurant == null) return NotFound();

            return View(restaurant);
        }

        // POST: Restaurants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant != null)
            {
                _context.Restaurants.Remove(restaurant);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RestaurantExists(int id)
        {
            return _context.Restaurants.Any(e => e.RestaurantKey == id);
        }

        private async Task CreateOpeningProject(int restaurantKey)
        {
            var restaurant = await _context.Restaurants.FindAsync(restaurantKey);
            if (restaurant == null) return;

            var project = new RestaurantOpeningProject
            {
                RestaurantKey = restaurantKey,
                ProjectName = $"{restaurant.RestaurantName} - Opening Project",
                ProjectStartDate = DateTime.Now,
                ProjectStatus = "Planning"
            };

            _context.RestaurantOpeningProjects.Add(project);
            await _context.SaveChangesAsync();

            // Create task executions for all standard tasks
            var standardTasks = await _context.RestaurantOpeningTasks.ToListAsync();
            var taskExecutions = new List<TaskExecution>();

            foreach (var task in standardTasks)
            {
                var execution = new TaskExecution
                {
                    ProjectId = project.ProjectId,
                    TaskId = task.TaskId,
                    Status = "Not Started"
                };
                taskExecutions.Add(execution);
            }

            _context.TaskExecutions.AddRange(taskExecutions);
            await _context.SaveChangesAsync();
        }
    }
}
