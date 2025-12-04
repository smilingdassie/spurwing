using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PosItemVerificationWeb.Data;
using PosItemVerificationWeb.Models;
using PosItemVerificationWeb.Services;
using PosItemVerificationWeb.ViewModels;

namespace PosItemVerificationWeb.Controllers
{


    namespace PosItemVerificationWeb.Controllers
    {
        public class RestaurantEventsController : Controller
        {
            private readonly RestaurantOpeningContext _context;
            private readonly RestaurantEventService _eventService;


            public RestaurantEventsController(RestaurantOpeningContext context,
    RestaurantEventService eventService)
            {
                _context = context;

                _eventService = eventService;
            }

            //GET: Dashboard
            // GET: RestaurantEvents/Dashboard
            public async Task<IActionResult> Dashboard()
            {
                var restaurants = await _context.Restaurants
                    .OrderBy(r => r.RestaurantName)
                    .ToListAsync();

                var allEvents = await _context.RestaurantEvents
                    .OrderBy(e => e.EventCode)
                    .ToListAsync();

                var dashboardData = new List<RestaurantDashboardViewModel>();

                foreach (var restaurant in restaurants)
                {
                    // Get execution statuses for this restaurant
                    var executions = await _context.RestaurantEventExecutions
                        .Where(e => e.RestaurantKey == restaurant.RestaurantKey)
                        .ToListAsync();

                    var eventStatuses = new List<EventStatusItem>();

                    

                    var completed = eventStatuses.Count(e => e.Status == "Completed");
                    var inProgress = eventStatuses.Count(e => e.Status == "In Progress");
                    var pending = eventStatuses.Count(e => e.Status == "Pending");
                    var total = eventStatuses.Count;

                    dashboardData.Add(
                        RestaurantDashboardViewModel.FromEntity(
                            restaurant,
                            executions,
                            allEvents
                        )
                    );

                   
                }

                return View(dashboardData);
            }

            // GET: RestaurantEvents
            public async Task<IActionResult> Index(int? restaurantKey)
            {


                ViewBag.Executions = await _context.RestaurantEventExecutions
                        .Where(e => e.RestaurantKey == restaurantKey)
                        .ToListAsync();


                ViewBag.AllEvents = await _context.RestaurantEvents
                    .OrderBy(e => e.EventCode)
                    .ToListAsync();

                // Get all restaurants for dropdown
                ViewBag.Restaurants = await _context.Restaurants
                    .OrderBy(r => r.RestaurantName)
                    .Select(r => new SelectListItem
                    {
                        Value = r.RestaurantKey.ToString(),
                        Text = $"{r.DCLink} - {r.RestaurantName}"
                    })
                    .ToListAsync();

                if (restaurantKey == null)
                {
                    // No restaurant selected, show instruction
                    return View("SelectRestaurant");
                }

                // Get selected restaurant details
                var restaurant = await _context.Restaurants
                    .FirstOrDefaultAsync(r => r.RestaurantKey == restaurantKey);

                if (restaurant == null)
                {
                    return NotFound();
                }

                ViewBag.SelectedRestaurant = restaurant;
                ViewBag.SelectedRestaurantKey = restaurantKey;

                // Get all master events with their execution status for this restaurant
                var eventProgress = await (
                    from evt in _context.RestaurantEvents
                    join exec in _context.RestaurantEventExecutions
                        on new { EventID = evt.ID, RestaurantKey = restaurantKey.Value }
                        equals new { exec.EventID, exec.RestaurantKey }
                        into execGroup
                    from exec in execGroup.DefaultIfEmpty()
                    orderby evt.EventCode
                    select new VwRestaurantEventProgress
                    {
                        RestaurantKey = restaurantKey.Value,
                        DCLink = restaurant.DCLink,
                        RestaurantName = restaurant.RestaurantName,
                        BrandName = restaurant.BrandName,
                        EventID = evt.ID,
                        EventCode = evt.EventCode,
                        EventName = evt.EventName,
                        EventDescription = evt.EventDescription,
                        ActionName = evt.ActionName,
                        ActionDescription = evt.ActionDescription,
                        MasterStatus = evt.StatusName,
                        ExecutionID = exec != null ? exec.ExecutionID : (int?)null,
                        ExecutionStatus = exec != null ? exec.Status : "Pending",
                        StartedDate = exec != null ? exec.StartedDate : (DateTime?)null,
                        CompletedDate = exec != null ? exec.CompletedDate : (DateTime?)null,
                        CompletedBy = exec != null ? exec.CompletedBy : null,
                        Notes = exec != null ? exec.Notes : null
                    }
                ).ToListAsync();

                return View(eventProgress);
            }


            [HttpPost]
            public async Task<IActionResult> ExecuteAction(int restaurantKey, int eventId, string actionName)
            {
                try
                {
                    await _eventService.ExecuteEventActionAsync(restaurantKey, eventId, actionName);

                    return Json(new
                    {
                        success = true,
                        message = $"{actionName} completed successfully."
                    });
                }
                catch (Exception ex)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Error: " + ex.Message
                    });
                }
            }


            // POST: RestaurantEvents/ExecuteAction
            [HttpPost]
            public async Task<IActionResult> ExecuteActionClaude(int restaurantKey, int eventId, string actionName)
            {
                var restaurant = await _context.Restaurants
                    .FirstOrDefaultAsync(r => r.RestaurantKey == restaurantKey);

                var eventItem = await _context.RestaurantEvents
                    .FirstOrDefaultAsync(e => e.ID == eventId);

                if (restaurant == null || eventItem == null)
                {
                    return NotFound();
                }

                // Check if execution record exists
                var execution = await _context.RestaurantEventExecutions
                    .FirstOrDefaultAsync(e => e.RestaurantKey == restaurantKey && e.EventID == eventId);

                if (execution == null)
                {
                    // Create new execution record
                    execution = new RestaurantEventExecution
                    {
                        RestaurantKey = restaurantKey,
                        EventID = eventId,
                        Status = "In Progress",
                        StartedDate = DateTime.Now,
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now
                    };
                    _context.RestaurantEventExecutions.Add(execution);
                }
                else
                {
                    // Update existing
                    execution.Status = "In Progress";
                    execution.ModifiedDate = DateTime.Now;
                    if (execution.StartedDate == null)
                    {
                        execution.StartedDate = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();

                // TODO: Route to appropriate stored procedure based on actionName

                return Json(new
                {
                    success = true,
                    message = $"Action '{actionName}' started for {restaurant.RestaurantName}",
                    executionId = execution.ExecutionID
                });
            }

            // POST: RestaurantEvents/CompleteAction
            [HttpPost]
            public async Task<IActionResult> CompleteAction(int executionId, string completedBy, string notes)
            {
                var execution = await _context.RestaurantEventExecutions
                    .FirstOrDefaultAsync(e => e.ExecutionID == executionId);

                if (execution == null)
                {
                    return NotFound();
                }

                execution.Status = "Completed";
                execution.CompletedDate = DateTime.Now;
                execution.CompletedBy = completedBy;
                execution.Notes = notes;
                execution.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Event completed successfully" });
            }

            // GET: RestaurantEvents/MasterEvents (for managing the master event list)
            public async Task<IActionResult> MasterEvents()
            {
                var events = await _context.RestaurantEvents
                    .OrderBy(e => e.EventCode)
                    .ToListAsync();

                return View(events);
            }
        }
    }
}
/*
    public RestaurantEventsController(RestaurantOpeningContext context)
        {
            _context = context;
        }

        // GET: RestaurantEvents
        public async Task<IActionResult> Index()
        {
            var events = await _context.RestaurantEvents
                .OrderBy(e => e.EventCode)
                .ToListAsync();

            return View(events);
        }

        // POST: RestaurantEvents/ExecuteAction
        [HttpPost]
        public async Task<IActionResult> ExecuteAction(int id, string actionName)
        {
            var restaurantEvent = await _context.RestaurantEvents
                .FirstOrDefaultAsync(e => e.ID == id);

            if (restaurantEvent == null)
            {
                return NotFound();
            }

            // TODO: Route to appropriate stored procedure based on actionName
            // This will be implemented when you connect the modals to stored procs

            return Json(new { success = true, message = $"Action '{actionName}' executed successfully" });
        }
    }
}
}
*/