// Services/IPOSVerificationService.cs
 
using PosItemVerificationWeb.Models;

namespace PosItemVerificationWeb.Services
{
    public interface IPOSVerificationService
    {
        Task<List<POSVerificationGroup>> GetVerificationGroupsAsync();
        Task<POSVerificationSummary> GetSummaryAsync();
        Task<ActionUpdateResult> SubmitActionsAsync(SubmitActionsRequest request);


            Task<bool> SendEmailNotificationAsync(SubmitActionsRequest request, ActionUpdateResult result);

        Task<bool> RollbackActionsAsync(Guid sessionId, string requestedBy);


    }
}
