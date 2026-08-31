using BlazorQuizApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazorQuizApp.Services
{
    public static class DbInitializer
    {
        public static async Task SeedAdminAsync(IServiceProvider serviceProvider, IConfiguration configuration, ILogger logger)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Admin", "User", "Pro" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    logger.LogInformation("Creating role: {Role}", role);
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var adminEmail = configuration["AppSettings:AdminEmail"];
            var adminPassword = configuration["AppSettings:AdminPassword"];

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                logger.LogInformation("Admin credentials are not configured; skipping admin user creation.");
                return;
            }

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                logger.LogInformation("Creating admin user: {Email}", adminEmail);
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "System Administrator"
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    logger.LogInformation("Admin user created and assigned to Admin role.");
                }
                else
                {
                    logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        public static async Task SeedSampleQuizResultsAsync(IServiceProvider serviceProvider, ILogger logger)
        {
            // Implementation for seeding sample quiz results if needed
            logger.LogInformation("Seeding sample quiz results...");
            // Add your seeding logic here
            await Task.CompletedTask;
        }

        public static async Task SeedSampleQuestionsAsync(IServiceProvider serviceProvider, ILogger logger)
        {
            // Implementation for seeding sample questions if needed
            logger.LogInformation("Seeding sample questions...");
            // Add your seeding logic here
            await Task.CompletedTask;
        }
    }
}
