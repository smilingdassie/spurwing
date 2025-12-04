using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PosItemVerificationWeb.Data;
namespace PosItemVerificationWeb.Services
{
 

    public class RestaurantEventService
    {
        private readonly RestaurantOpeningContext _context;

        public RestaurantEventService(RestaurantOpeningContext context)
        {
            _context = context;
        }

        public async Task<bool> ExecuteEventActionAsync(int restaurantKey, int eventId, string actionName)
        {
            var sql = "EXEC dbo.MarkEventAsCompleted @RestaurantKey, @EventID, @ActionName";

            var p1 = new SqlParameter("@RestaurantKey", restaurantKey);
            var p2 = new SqlParameter("@EventID", eventId);
            var p3 = new SqlParameter("@ActionName", actionName);

            await _context.Database.ExecuteSqlRawAsync(sql, p1, p2, p3);

            return true;
        }
    }

}
