using BlazorQuizApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorQuizApp.Tests.Helpers
{
    /// <summary>
    /// Creates isolated in-memory EF Core contexts for testing.
    /// Each call to Create() uses a unique database name to prevent test interference.
    /// Note: The in-memory provider does NOT enforce unique indexes (e.g., Subscription.UserId),
    /// so constraint violations that would fail on SQL Server may pass here.
    /// </summary>
    public static class DbContextHelper
    {
        public static ApplicationDbContext Create(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        /// <summary>
        /// Builds a full ServiceProvider with an isolated in-memory ApplicationDbContext.
        /// Use this for services that resolve ApplicationDbContext via IServiceScopeFactory
        /// (e.g. SubscriptionService).
        /// </summary>
        public static ServiceProvider CreateServiceProvider(string? dbName = null,
            Action<ApplicationDbContext>? seed = null)
        {
            var services = new ServiceCollection();
            var name = dbName ?? Guid.NewGuid().ToString();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(name));

            services.AddLogging();

            var provider = services.BuildServiceProvider();

            if (seed != null)
            {
                using var scope = provider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                seed(context);
                context.SaveChanges();
            }

            return provider;
        }
    }
}
