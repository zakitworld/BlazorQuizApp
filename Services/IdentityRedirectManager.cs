using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace BlazorQuizApp.Services
{
    public sealed class IdentityRedirectManager(NavigationManager navigationManager)
    {
        public const string StatusCookieName = "Identity.StatusMessage";

        private static readonly CookieOptions StatusCookieOptions = new() { Path = "/", HttpOnly = true, SameSite = SameSiteMode.Strict };

        [DoesNotReturn]
        public void RedirectTo(string? uri)
        {
            uri ??= "";

            // Prevent open redirect attacks by ensuring the URI is local
            if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
            {
                uri = navigationManager.ToBaseRelativePath(uri);
            }

            // This will throw a NavigationException and stop execution of the current component
            navigationManager.NavigateTo(uri);
            throw new InvalidOperationException("Navigation should have happened.");
        }

        [DoesNotReturn]
        public void RedirectToWithStatus(string uri, string message, HttpContext context)
        {
            context.Response.Cookies.Append(StatusCookieName, message, StatusCookieOptions);
            RedirectTo(uri);
        }

        private string GetCurrentPath() => navigationManager.ToBaseRelativePath(navigationManager.Uri);

        [DoesNotReturn]
        public void RedirectToCurrentPage() => RedirectTo(GetCurrentPath());

        [DoesNotReturn]
        public void RedirectToCurrentPageWithStatus(string message, HttpContext context)
            => RedirectToWithStatus(GetCurrentPath(), message, context);
    }
}
