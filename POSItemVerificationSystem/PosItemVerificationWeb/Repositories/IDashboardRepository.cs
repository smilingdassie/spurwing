using Microsoft.Data.SqlClient;
using PosItemVerificationWeb.Models;
namespace PosItemVerificationWeb.Repositories
{
    // Repositories/IDashboardRepository.cs
    public interface IDashboardRepository
    {
        Task<List<SalesForceMetric>> GetSalesForceMetricsAsync();
        Task<List<CrmMetric>> GetCrmMetricsAsync();
        Task<List<DashboardSnapshot>> GetMetricsHistoryAsync(DateTime since);
        Task SaveSnapshotAsync(DashboardSnapshot snapshot);
        Task DeleteSnapshotsOlderThanAsync(DateTime cutoffDate);
    }


}
