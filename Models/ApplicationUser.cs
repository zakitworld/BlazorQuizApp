using Microsoft.AspNetCore.Identity;

namespace BlazorQuizApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Add any additional properties here
        public string? FullName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
