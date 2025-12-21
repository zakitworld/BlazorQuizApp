using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BlazorQuizApp.Configuration;
using BlazorQuizApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BlazorQuizApp.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAdminAsync(IServiceProvider services, IConfiguration config, ILogger logger)
        {
            using var scope = services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var appSettings = scope.ServiceProvider.GetRequiredService<IOptions<AppSettings>>().Value;

            var adminRole = "Admin";
            try
            {
                if (!await roleManager.RoleExistsAsync(adminRole))
                {
                    await roleManager.CreateAsync(new IdentityRole(adminRole));
                    logger.LogInformation("Admin role created.");
                }

                var adminEmail = appSettings.AdminUser.Email;
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

        public static async Task SeedSampleQuizResultsAsync(IServiceProvider services, ILogger logger)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                logger.LogInformation("Starting to seed sample quiz results...");
                
                // Check if we already have quiz results
                var existingResults = await context.QuizResults.CountAsync();
                logger.LogInformation($"Found {existingResults} existing quiz results in database.");
                
                if (existingResults > 0)
                {
                    logger.LogInformation($"Database already contains {existingResults} quiz results. Skipping sample data seeding.");
                    return;
                }

                logger.LogInformation("Creating sample quiz results...");

                // Create sample quiz results
                var sampleResults = new List<QuizResult>
                {
                    new QuizResult
                    {
                        UserId = "sample-user-1",
                        UserName = "John Doe",
                        TotalQuestions = 10,
                        CorrectAnswers = 9,
                        CompletedAt = DateTime.UtcNow.AddDays(-1),
                        Duration = TimeSpan.FromMinutes(8)
                    },
                    new QuizResult
                    {
                        UserId = "sample-user-2",
                        UserName = "Jane Smith",
                        TotalQuestions = 10,
                        CorrectAnswers = 8,
                        CompletedAt = DateTime.UtcNow.AddDays(-2),
                        Duration = TimeSpan.FromMinutes(10)
                    },
                    new QuizResult
                    {
                        UserId = "sample-user-3",
                        UserName = "Mike Johnson",
                        TotalQuestions = 10,
                        CorrectAnswers = 7,
                        CompletedAt = DateTime.UtcNow.AddDays(-3),
                        Duration = TimeSpan.FromMinutes(12)
                    },
                    new QuizResult
                    {
                        UserId = "sample-user-4",
                        UserName = "Sarah Wilson",
                        TotalQuestions = 10,
                        CorrectAnswers = 6,
                        CompletedAt = DateTime.UtcNow.AddDays(-4),
                        Duration = TimeSpan.FromMinutes(15)
                    },
                    new QuizResult
                    {
                        UserId = "sample-user-5",
                        UserName = "David Brown",
                        TotalQuestions = 10,
                        CorrectAnswers = 5,
                        CompletedAt = DateTime.UtcNow.AddDays(-5),
                        Duration = TimeSpan.FromMinutes(18)
                    }
                };

                logger.LogInformation("Adding {Count} sample quiz results to database...", sampleResults.Count);
                await context.QuizResults.AddRangeAsync(sampleResults);
                
                logger.LogInformation("Saving changes to database...");
                await context.SaveChangesAsync();

                logger.LogInformation("Successfully seeded {Count} sample quiz results.", sampleResults.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while seeding sample quiz results. Exception: {ExceptionMessage}", ex.Message);
                if (ex.InnerException != null)
                {
                    logger.LogError("Inner exception: {InnerExceptionMessage}", ex.InnerException.Message);
                }
            }
        }

        public static async Task SeedSampleQuestionsAsync(IServiceProvider services, ILogger logger)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                logger.LogInformation("Starting to seed sample questions...");
                
                // Check if we already have questions
                var existingQuestions = await context.QuizQuestions.CountAsync();
                logger.LogInformation($"Found {existingQuestions} existing questions in database.");
                
                if (existingQuestions > 0)
                {
                    logger.LogInformation($"Database already contains {existingQuestions} questions. Skipping sample data seeding.");
                    return;
                }

                logger.LogInformation("Creating sample questions...");

                // Create sample questions
                var questions = new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        QuestionText = "Which of the following statements is true regarding dependency injection?",
                        Category = "ASP.NET Core",
                        Difficulty = "Medium",
                        CreatedAt = DateTime.UtcNow,
                        Options = new List<QuizOption>
                        {
                            new QuizOption { Text = "Dependency injection is a design pattern that allows you to loosely couple your app's dependencies." },
                            new QuizOption { Text = "ASP.NET Core comes with a built-in service container that supports dependency injection." },
                            new QuizOption { Text = "ASP.NET Core's service container supports three service lifetimes: transient, scoped, and singleton." },
                            new QuizOption { Text = "All of the above." }
                        }
                    },
                    new QuizQuestion
                    {
                        QuestionText = "What is Blazor?",
                        Category = "Blazor",
                        Difficulty = "Easy",
                        CreatedAt = DateTime.UtcNow,
                        Options = new List<QuizOption>
                        {
                            new QuizOption { Text = "A JavaScript framework" },
                            new QuizOption { Text = "A .NET framework for building interactive web UIs using C#" },
                            new QuizOption { Text = "A database management system" },
                            new QuizOption { Text = "A cloud computing platform" }
                        }
                    },
                    new QuizQuestion
                    {
                        QuestionText = "What is Entity Framework Core?",
                        Category = "Entity Framework",
                        Difficulty = "Easy",
                        CreatedAt = DateTime.UtcNow,
                        Options = new List<QuizOption>
                        {
                            new QuizOption { Text = "A web framework" },
                            new QuizOption { Text = "An object-relational mapping (ORM) framework" },
                            new QuizOption { Text = "A testing framework" },
                            new QuizOption { Text = "A logging framework" }
                        }
                    }
                };

                // Add questions to database first
                await context.QuizQuestions.AddRangeAsync(questions);
                await context.SaveChangesAsync();

                // Now set the correct option IDs after the options have been saved and have proper IDs
                questions[0].CorrectOptionId = questions[0].Options[3].Id; // "All of the above" (4th option)
                questions[1].CorrectOptionId = questions[1].Options[1].Id; // "A .NET framework..." (2nd option)
                questions[2].CorrectOptionId = questions[2].Options[1].Id; // "An object-relational mapping..." (2nd option)

                await context.SaveChangesAsync();

                logger.LogInformation("Successfully seeded {Count} sample questions with correct option IDs.", questions.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while seeding sample questions. Exception: {ExceptionMessage}", ex.Message);
                if (ex.InnerException != null)
                {
                    logger.LogError("Inner exception: {InnerExceptionMessage}", ex.InnerException.Message);
                }
            }
        }
    }
} 