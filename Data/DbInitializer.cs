using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BlazorQuizApp.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAdminAsync(IServiceProvider services, IConfiguration config, ILogger logger)
        {
            using var scope = services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var adminRole = "Admin";
            try
            {
                if (!await roleManager.RoleExistsAsync(adminRole))
                {
                    await roleManager.CreateAsync(new IdentityRole(adminRole));
                    logger.LogInformation("Admin role created.");
                }

                var adminEmail = config["AdminUser:Email"];
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser != null && adminUser.EmailConfirmed)
                {
                    if (!await userManager.IsInRoleAsync(adminUser, adminRole))
                    {
                        await userManager.AddToRoleAsync(adminUser, adminRole);
                        logger.LogInformation($"Admin role assigned to {adminEmail}.");
                    }
                }
                else
                {
                    logger.LogWarning($"Admin user {adminEmail} not found or not confirmed.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while seeding admin role/user.");
            }
        }
    }
} 