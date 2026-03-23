using BlazorQuizApp.Models;
using BlazorQuizApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
                return TypedResults.LocalRedirect($"~/{returnUrl}");
            });

            accountGroup.MapPost("/PerformLogin", async (
                [FromForm] string email,
                [FromForm] string password,
                [FromForm] bool? rememberMe,
                [FromQuery] string? returnUrl,
                SignInManager<ApplicationUser> signInManager,
                IdentityRedirectManager redirectManager) =>
            {
                var result = await signInManager.PasswordSignInAsync(email, password, rememberMe ?? false, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return TypedResults.LocalRedirect($"~/{returnUrl ?? "/"}");
                }
                
                // On failure, redirect back to login with error
                return TypedResults.Redirect($"/Account/Login?error=InvalidLoginAttempt&returnUrl={returnUrl}");
            });

            accountGroup.MapPost("/PerformRegister", async (
                [FromForm] string email,
                [FromForm] string password,
                [FromForm] string fullName,
                [FromQuery] string? returnUrl,
                UserManager<ApplicationUser> userManager,
                IUserStore<ApplicationUser> userStore,
                SignInManager<ApplicationUser> signInManager) =>
            {
                var user = new ApplicationUser { FullName = fullName };
                await userStore.SetUserNameAsync(user, email, CancellationToken.None);
                var emailStore = (IUserEmailStore<ApplicationUser>)userStore;
                await emailStore.SetEmailAsync(user, email, CancellationToken.None);

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await signInManager.SignInAsync(user, isPersistent: false);
                    return TypedResults.LocalRedirect($"~/{returnUrl ?? "/"}");
                }

                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                return TypedResults.Redirect($"/Account/Register?error={Uri.EscapeDataString(errors)}&returnUrl={returnUrl}");
            });

            return accountGroup;
        }
    }
}
