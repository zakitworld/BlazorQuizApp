using BlazorQuizApp.Components;
using BlazorQuizApp.Components.Account;
using BlazorQuizApp.Configuration;
using BlazorQuizApp.Data;
using BlazorQuizApp.Middleware;
using BlazorQuizApp.Models;
using BlazorQuizApp.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using AspNetCoreRateLimit;

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

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // Configure strongly-typed settings
    builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
    builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
    builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

    // Rate Limiting Configuration
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

    // Response Compression
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });

    // Memory Cache for SmartASP.NET (no Redis available)
    builder.Services.AddMemoryCache();

    // Health checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>();

    // Database Context Factory for Blazor Server
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));

    // Also add regular DbContext for Identity
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    // Application Services
    builder.Services.AddScoped<QuizService>();
    builder.Services.AddSingleton<INotificationService, NotificationService>();
    builder.Services.AddScoped<IQuizBusinessService, QuizBusinessService>();
    builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
    builder.Services.AddScoped<IStripePaymentService, StripePaymentService>();
    builder.Services.AddScoped<IEmailService, EmailService>();

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
            options.ApplicationCookie.Configure(opts =>
            {
                opts.Cookie.HttpOnly = true;
                opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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
            policy.WithOrigins(builder.Configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // Antiforgery enhancements
    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-CSRF-TOKEN";
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

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
    app.UseResponseCompression();

    app.UseHttpsRedirection();

    app.UseCors();

    app.UseAntiforgery();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // Health check endpoints
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") });
    app.MapHealthChecks("/health/live", new() { Predicate = _ => false });

    // Stripe webhook endpoint
    app.MapPost("/api/stripe/webhook", async (HttpContext context, IStripePaymentService stripeService) =>
    {
        var json = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var signature = context.Request.Headers["Stripe-Signature"].ToString();

        var success = await stripeService.HandleWebhookAsync(json, signature);
        return success ? Results.Ok() : Results.BadRequest();
    });

    // Additional endpoints required by the Identity /Account Razor components.
    app.MapAdditionalIdentityEndpoints();

    // Ensure database is created and run migrations
    using (var scope = app.Services.CreateScope())
    {
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
        // Extract token from confirmation link
        var uri = new Uri(confirmationLink);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var token = query["code"] ?? "";

        await _emailService.SendEmailVerificationAsync(email, user.Id, token);
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        // Extract token from reset link
        var uri = new Uri(resetLink);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var token = query["code"] ?? "";

        await _emailService.SendPasswordResetAsync(email, token);
    }

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        await _emailService.SendPasswordResetAsync(email, resetCode);
    }
}
