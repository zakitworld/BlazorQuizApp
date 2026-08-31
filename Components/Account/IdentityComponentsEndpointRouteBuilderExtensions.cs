using BlazorQuizApp.Models;
using BlazorQuizApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Microsoft.AspNetCore.Routing
{
    internal static class IdentityComponentsEndpointRouteBuilderExtensions
    {
        // These endpoints are required by the Identity /Account Razor components.
        public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            var accountGroup = endpoints.MapGroup("/Account");

            accountGroup.MapPost("/Logout", async (
                ClaimsPrincipal user,
                SignInManager<ApplicationUser> signInManager,
                [FromForm] string returnUrl) =>
            {
                await signInManager.SignOutAsync();
                return TypedResults.LocalRedirect(NormalizeLocalReturnUrl(returnUrl));
            }).RequireAuthorization();

            accountGroup.MapPost("/PerformLogin", async (
                [FromForm] string email,
                [FromForm] string password,
                [FromForm] bool? rememberMe,
                [FromQuery] string? returnUrl,
                SignInManager<ApplicationUser> signInManager,
                IAuditService auditService) =>
            {
                var result = await signInManager.PasswordSignInAsync(email, password, rememberMe ?? false, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    await auditService.WriteAsync("LoginSucceeded", userName: email);
                    return TypedResults.LocalRedirect(NormalizeLocalReturnUrl(returnUrl));
                }
                await auditService.WriteAsync("LoginFailed", userName: email);
                
                // On failure, redirect back to login with error
                return TypedResults.Redirect($"/Account/Login?error=InvalidLoginAttempt&returnUrl={Uri.EscapeDataString(NormalizeLocalReturnUrl(returnUrl))}");
            });

            accountGroup.MapPost("/PerformRegister", async (
                [FromForm] string email,
                [FromForm] string password,
                [FromForm] string confirmPassword,
                [FromForm] string fullName,
                [FromQuery] string? returnUrl,
                UserManager<ApplicationUser> userManager,
                IUserStore<ApplicationUser> userStore,
                IEmailSender<ApplicationUser> emailSender,
                LinkGenerator links,
                HttpContext httpContext,
                IWebHostEnvironment environment,
                IConfiguration configuration,
                IAuditService auditService) =>
            {
                email = email.Trim();
                fullName = fullName.Trim();
                if (password != confirmPassword || fullName.Length is < 2 or > 100)
                {
                    return TypedResults.Redirect("/Account/Register?error=InvalidRegistrationData");
                }

                var emailDeliveryConfigured = !string.IsNullOrWhiteSpace(
                    configuration["EmailSettings:SendGridApiKey"]);

                // A previous development registration may have created the user before
                // email delivery failed. Let that same user retry without deleting data
                // or weakening the production confirmation requirement.
                var existingUser = await userManager.FindByEmailAsync(email);
                if (environment.IsDevelopment() && !emailDeliveryConfigured && existingUser != null &&
                    !existingUser.EmailConfirmed && await userManager.CheckPasswordAsync(existingUser, password))
                {
                    var existingToken = await userManager.GenerateEmailConfirmationTokenAsync(existingUser);
                    await userManager.ConfirmEmailAsync(existingUser, existingToken);
                    await auditService.WriteAsync("DevelopmentEmailAutoConfirmed", nameof(ApplicationUser),
                        userId: existingUser.Id, userName: email);
                    return TypedResults.LocalRedirect("~/Account/Login?confirmed=true");
                }

                var user = new ApplicationUser { FullName = fullName };
                await userStore.SetUserNameAsync(user, email, CancellationToken.None);
                var emailStore = (IUserEmailStore<ApplicationUser>)userStore;
                await emailStore.SetEmailAsync(user, email, CancellationToken.None);

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

                    if (environment.IsDevelopment() && !emailDeliveryConfigured)
                    {
                        await userManager.ConfirmEmailAsync(user, token);
                        await auditService.WriteAsync("DevelopmentEmailAutoConfirmed", nameof(ApplicationUser),
                            userId: user.Id, userName: email);
                        return TypedResults.LocalRedirect("~/Account/Login?confirmed=true");
                    }

                    var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                    var confirmationLink = links.GetUriByName(httpContext, "ConfirmEmail",
                        new { userId = user.Id, code })!;
                    await emailSender.SendConfirmationLinkAsync(user, email, confirmationLink);
                    await auditService.WriteAsync("AccountRegistered", nameof(ApplicationUser),
                        userId: user.Id, userName: email);
                    return TypedResults.LocalRedirect("~/Account/Login?registered=true");
                }

                return TypedResults.Redirect("/Account/Register?error=RegistrationFailed");
            });

            accountGroup.MapGet("/ConfirmEmail", async (string userId, string code,
                UserManager<ApplicationUser> userManager, IAuditService auditService) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null) return Results.LocalRedirect("/Account/Login?confirmed=false");
                var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                var result = await userManager.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                    await auditService.WriteAsync("EmailConfirmed", nameof(ApplicationUser),
                        userId: user.Id, userName: user.Email);
                return Results.LocalRedirect($"/Account/Login?confirmed={result.Succeeded.ToString().ToLowerInvariant()}");
            }).WithName("ConfirmEmail");

            accountGroup.MapPost("/ForgotPassword", async ([FromForm] string email,
                UserManager<ApplicationUser> userManager, IEmailSender<ApplicationUser> emailSender,
                LinkGenerator links, HttpContext httpContext) =>
            {
                var user = await userManager.FindByEmailAsync(email.Trim());
                if (user != null && await userManager.IsEmailConfirmedAsync(user))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                    var resetLink = links.GetUriByName(httpContext, "ResetPassword",
                        new { email = user.Email, code })!;
                    await emailSender.SendPasswordResetLinkAsync(user, user.Email!, resetLink);
                }
                return Results.LocalRedirect("/Account/ForgotPassword?sent=true");
            });

            accountGroup.MapGet("/ResetPassword", (string email, string code) =>
                Results.LocalRedirect($"/Account/ResetPasswordForm?email={Uri.EscapeDataString(email)}&code={Uri.EscapeDataString(code)}"))
                .WithName("ResetPassword");

            accountGroup.MapPost("/ResetPassword", async ([FromForm] string email, [FromForm] string code,
                [FromForm] string password, [FromForm] string confirmPassword,
                UserManager<ApplicationUser> userManager, IAuditService auditService) =>
            {
                if (password != confirmPassword)
                    return Results.LocalRedirect("/Account/ResetPasswordForm?error=invalid");
                var user = await userManager.FindByEmailAsync(email);
                if (user == null) return Results.LocalRedirect("/Account/Login?reset=true");
                var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                var result = await userManager.ResetPasswordAsync(user, token, password);
                if (result.Succeeded)
                    await auditService.WriteAsync("PasswordReset", nameof(ApplicationUser), userId: user.Id, userName: user.Email);
                return Results.LocalRedirect(result.Succeeded ? "/Account/Login?reset=true" : "/Account/ResetPasswordForm?error=invalid");
            });

            return accountGroup;
        }

        private static string NormalizeLocalReturnUrl(string? returnUrl) =>
            !string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//")
                ? returnUrl
                : "/";
    }
}
