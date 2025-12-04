using System.ComponentModel.DataAnnotations;

namespace PosItemVerificationWeb.Models
{
    public class RestaurantEvent
    {
        [Key]
        public int ID { get; set; }

        [StringLength(50)]
        public string? EventCode { get; set; }

        [StringLength(200)]
        public string? EventName { get; set; }

        [StringLength(500)]
        public string? EventDescription { get; set; }

        [StringLength(200)]
        public string? ActionName { get; set; }

        [StringLength(500)]
        public string? ActionDescription { get; set; }

        [StringLength(500)]
        public string? StatusName { get; set; }
    }
}
