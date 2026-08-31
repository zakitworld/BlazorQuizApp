namespace BlazorQuizApp.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityHeadersMiddleware> _logger;

        public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Remove server header
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            // Security headers
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

            // Content Security Policy
            var csp = "default-src 'self'; " +
                     "script-src 'self' 'unsafe-inline' https://js.paystack.co https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
                     "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.googleapis.com; " +
                     "font-src 'self' https://cdn.jsdelivr.net https://fonts.gstatic.com; " +
                     "img-src 'self' data: https:; " +
                     "connect-src 'self' https://api.paystack.co https://paystack.com; " +
                     "frame-src https://js.paystack.co; " +
                     "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none';";

            context.Response.Headers.Append("Content-Security-Policy", csp);

            await _next(context);
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
