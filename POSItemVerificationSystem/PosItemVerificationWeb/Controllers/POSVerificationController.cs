// Controllers/POSVerificationController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PosItemVerificationWeb.Models;
using PosItemVerificationWeb.Services;
using PosItemVerificationWeb.Helpers;

namespace PosItemVerificationWeb.Controllers
{


     
        [Authorize]
        public class POSItemVerificationController : Controller
        {
            private readonly IPOSVerificationService _posService;
            private readonly ILogger<POSItemVerificationController> _logger;
        private readonly AllowedUsersConfig _allowed;
        public POSItemVerificationController(IPOSVerificationService posService, ILogger<POSItemVerificationController> logger, IOptions<AllowedUsersConfig> allowed)
            {
                _posService = posService;
                _logger = logger;
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
                    var groups = await _posService.GetVerificationGroupsAsync();
                    var summary = await _posService.GetSummaryAsync();

                    ViewBag.Summary = summary;
                    ViewBag.UserName = User.Identity?.Name ?? "Unknown User";
                    return View(groups);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading POS verification data for user {User}", User.Identity?.Name);
                    ViewBag.Error = "Unable to load verification data. Please check the database connection.";
                    return View(new List<POSVerificationGroup>());
                }
            }

            [HttpPost]
            public async Task<IActionResult> SubmitActions([FromBody] SubmitActionsRequest request)
            {
                try
                {
                    // Set the submitting user
                    request.SubmittedBy = User.Identity?.Name ?? "Unknown User";

                    _logger.LogInformation("User {User} submitting {Count} actions", request.SubmittedBy, request.Actions.Count);

                    var result = await _posService.SubmitActionsAsync(request);

                    if (result.Success)
                    {
                        _logger.LogInformation("Successfully processed {Count} actions for user {User}",
                            result.TotalItemsUpdated, request.SubmittedBy);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to process actions for user {User}: {Message}",
                            request.SubmittedBy, result.Message);
                    }

                    return Json(new
                    {
                        success = result.Success,
                        message = result.Message,
                        totalUpdated = result.TotalItemsUpdated,
                        updateCount = result.UpdateActions,
                        createCount = result.CreateActions,
                        errors = result.Errors,
                        sessionId = result.SessionId
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error submitting actions for user {User}", User.Identity?.Name);
                    return Json(new
                    {
                        success = false,
                        message = "An error occurred while submitting actions",
                        error = ex.Message
                    });
                }
            }

            [HttpGet]
            public async Task<IActionResult> GetSummary()
            {
                try
                {
                    var summary = await _posService.GetSummaryAsync();
                    return Json(summary);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting summary for user {User}", User.Identity?.Name);
                    return Json(new { error = "Unable to get summary data" });
                }
            }


        [HttpPost]
        public async Task<IActionResult> RollbackActions([FromBody] RollbackRequest request)
        {
            try
            {
                var success = await _posService.RollbackActionsAsync(request.SessionId, User.Identity?.Name ?? "Unknown");

                return Json(new
                {
                    success = success,
                    message = success ? "Changes rolled back successfully" : "No changes to rollback or rollback failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during rollback");
                return Json(new { success = false, message = "Rollback failed: " + ex.Message });
            }
        }

        public class RollbackRequest
        {
            public Guid SessionId { get; set; }
        }

    }
    }


    //public class POSVerificationController : Controller
    //{
    //    private readonly IPOSVerificationService _posService;
    //    private readonly ILogger<POSVerificationController> _logger;

    //    public POSVerificationController(IPOSVerificationService posService, ILogger<POSVerificationController> logger)
    //    {
    //        _posService = posService;
    //        _logger = logger;
    //    }

    //    public async Task<IActionResult> Index()
    //    {
    //        try
    //        {
    //            var groups = await _posService.GetVerificationGroupsAsync();
    //            var summary = await _posService.GetSummaryAsync();

    //            ViewBag.Summary = summary;
    //            return View(groups);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error loading POS verification data");
    //            ViewBag.Error = "Unable to load verification data. Please check the database connection.";
    //            return View(new List<POSVerificationGroup>());
    //        }
    //    }

    //    [HttpPost]
    //    public async Task<IActionResult> SubmitActions([FromBody] SubmitActionsRequest request)
    //    {
    //        try
    //        {
    //            var success = await _posService.SubmitActionsAsync(request);
    //            return Json(new { success = success, message = success.Success ? "Actions submitted successfully" : "Failed to submit actions" });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error submitting actions");
    //            return Json(new { success = false, message = "An error occurred while submitting actions" });
    //        }
    //    }

    //    [HttpGet]
    //    public async Task<IActionResult> GetSummary()
    //    {
    //        try
    //        {
    //            var summary = await _posService.GetSummaryAsync();
    //            return Json(summary);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error getting summary");
    //            return Json(new { error = "Unable to get summary data" });
    //        }
    //    }
    //}


