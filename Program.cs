using BlazorQuizApp.Components;
using BlazorQuizApp.Middleware;
using BlazorQuizApp.Models;
using BlazorQuizApp.Services;
using BlazorQuizApp.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using AspNetCoreRateLimit;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/blazorquizapp-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10_000_000)
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting Blazor Quiz App");

    // Validate BaseUrl configuration
    var baseUrl = builder.Configuration["AppSettings:BaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        Log.Warning("BaseUrl is not configured, using default: https://localhost:7233");
        baseUrl = "https://localhost:7233";
    }
    else
    {
        // Validate URL format
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            Log.Warning("Invalid BaseUrl format: {BaseUrl}, using default: https://localhost:7233", baseUrl);
            baseUrl = "https://localhost:7233";
        }
        else
        {
            Log.Information("Using BaseUrl: {BaseUrl}", baseUrl);
        }
    }

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    builder.Services.AddHttpContextAccessor();

    // Rate Limiting Configuration
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

    // Bind configuration sections to model classes
    builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
    builder.Services.Configure<PaystackSettings>(builder.Configuration.GetSection("Paystack"));

    // Response Compression
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });

    // Memory Cache for SmartASP.NET (no Redis available)
    builder.Services.AddMemoryCache();

    // Database Context for Identity and scoped services
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    // Add DbContext as scoped (automatically registers IDbContextFactory as Singleton)
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    // Application Services
    builder.Services.AddSingleton<INotificationService, NotificationService>();
    builder.Services.AddSingleton<ICacheService, CacheService>();
    builder.Services.AddSingleton<IValidationService, ValidationService>();
    builder.Services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();
    builder.Services.AddScoped<IQuizBusinessService, QuizBusinessService>();
    builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
    builder.Services.AddScoped<IPaystackPaymentService, PaystackPaymentService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IAuditService, AuditService>();
    builder.Services.AddScoped<IAssignmentService, AssignmentService>();
    builder.Services.AddScoped<IQuestionStudioService, QuestionStudioService>();

    // Identity & Authentication
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddScoped<IdentityUserAccessor>();
    builder.Services.AddScoped<IdentityRedirectManager>();
    builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

    builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        })
        .AddIdentityCookies(options =>
        {
            options.ApplicationCookie?.Configure(opts =>
            {
                opts.Cookie.HttpOnly = true;
                opts.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
                opts.Cookie.SameSite = SameSiteMode.Strict;
                opts.ExpireTimeSpan = TimeSpan.FromDays(30);
                opts.SlidingExpiration = true;
            });
        });

    builder.Services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = true;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

    // Use real email sender instead of NoOp
    builder.Services.AddScoped<IEmailSender<ApplicationUser>>(sp =>
    {
        var emailService = sp.GetRequiredService<IEmailService>();
        return new IdentityEmailSender(emailService);
    });

    // CORS Configuration
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            var baseUrl = builder.Configuration["AppSettings:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = "https://localhost:7233";
            }

            // Ensure URL is valid and doesn't end with /
            baseUrl = baseUrl.TrimEnd('/');

            policy.WithOrigins(baseUrl)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // Antiforgery enhancements
    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-CSRF-TOKEN";
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });
    
    // Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>(name: "database", tags: new[] { "ready", "db" });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseMigrationsEndPoint();
    }
    else
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    // Security Headers Middleware
    app.UseSecurityHeaders();

    // Rate Limiting
    app.UseIpRateLimiting();

    // Response Compression
    // app.UseResponseCompression();

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseCors();

    app.UseAntiforgery();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // Health check endpoints
    app.MapHealthChecks("/health").RequireAuthorization(policy => policy.RequireRole("Admin"));
    app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") })
        .RequireAuthorization(policy => policy.RequireRole("Admin"));
    app.MapHealthChecks("/health/live", new() { Predicate = _ => false });

    // Paystack webhook endpoint
    app.MapPost("/api/paystack/webhook", async (HttpContext context, IPaystackPaymentService paystackService) =>
    {
        if (!context.Request.HasJsonContentType() || context.Request.ContentLength is > 1_048_576)
            return Results.BadRequest();

        var payload = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var signature = context.Request.Headers["x-paystack-signature"].ToString();

        var success = await paystackService.HandleWebhookAsync(payload, signature);
        return success ? Results.Ok() : Results.BadRequest();
    }).DisableAntiforgery();

    app.MapGet("/api/instructor/results.csv", async (ClaimsPrincipal user, ApplicationDbContext db) =>
    {
        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var rows = await db.QuizResults.AsNoTracking()
            .Where(r => r.QuizAssignment != null && r.QuizAssignment.OwnerId == ownerId)
            .Include(r => r.QuizAssignment).OrderByDescending(r => r.CompletedAt).ToListAsync();
        static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
        var csv = new StringBuilder("Assignment,Learner,Completed,Score,Passed,Attempt,Certificate\r\n");
        foreach (var row in rows)
            csv.AppendLine(string.Join(',', Csv(row.QuizAssignment!.Title), Csv(row.UserName),
                row.CompletedAt.ToString("O"), row.ScorePercentage.ToString("F2"), row.IsPassed,
                row.AttemptNumber, Csv(row.CertificateCode ?? string.Empty)));
        return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "assignment-results.csv");
    }).RequireAuthorization(policy => policy.RequireRole("Admin"));

    // Additional endpoints required by the Identity /Account Razor components.
    app.MapAdditionalIdentityEndpoints();

    // Development can apply migrations automatically. Production deployments should
    // apply a reviewed idempotent migration script before starting the web process.
    if (app.Environment.IsDevelopment() &&
        builder.Configuration.GetValue("Database:ApplyMigrations", true))
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            app.Logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync();
            app.Logger.LogInformation("Database migrations completed.");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Error applying database migrations");
        }
    }

    // Seed Admin role and assign to admin user
    await DbInitializer.SeedAdminAsync(app.Services, app.Configuration, app.Logger);

    // Seed sample quiz results for testing (only in development)
    if (app.Environment.IsDevelopment())
    {
        await DbInitializer.SeedSampleQuizResultsAsync(app.Services, app.Logger);
        await DbInitializer.SeedSampleQuestionsAsync(app.Services, app.Logger);
    }

    app.Run();
}
catch (HostAbortedException)
{
    // Expected when EF Core design-time tooling stops the temporary host.
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Identity Email Sender Adapter
public class IdentityEmailSender : IEmailSender<ApplicationUser>
{
    private readonly IEmailService _emailService;

    public IdentityEmailSender(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        await _emailService.SendEmailAsync(email, "Confirm your Quiz App account",
            $"<p>Please confirm your account by <a href=\"{System.Net.WebUtility.HtmlEncode(confirmationLink)}\">clicking here</a>.</p>");
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        await _emailService.SendEmailAsync(email, "Reset your Quiz App password",
            $"<p>Reset your password by <a href=\"{System.Net.WebUtility.HtmlEncode(resetLink)}\">clicking here</a>.</p>");
    }

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        await _emailService.SendPasswordResetAsync(email, resetCode);
    }
}
