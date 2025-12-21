using System.ComponentModel.DataAnnotations;

namespace BlazorQuizApp.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        [StringLength(256)]
        public string? UserName { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [StringLength(100)]
        public string? EntityType { get; set; }

        public int? EntityId { get; set; }

        [StringLength(2000)]
        public string? Details { get; set; }

        [StringLength(50)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
