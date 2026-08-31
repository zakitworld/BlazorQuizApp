using BlazorQuizApp.Models;
using System.Security.Claims;

namespace BlazorQuizApp.Services;

public interface IAuditService
{
    Task WriteAsync(string action, string? entityType = null, int? entityId = null,
        string? details = null, string? userId = null, string? userName = null);
}

public sealed class AuditService(
    ApplicationDbContext context,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditService> logger) : IAuditService
{
    public async Task WriteAsync(string action, string? entityType = null, int? entityId = null,
        string? details = null, string? userId = null, string? userName = null)
    {
        try
        {
            var http = httpContextAccessor.HttpContext;
            var principal = http?.User;
            context.AuditLogs.Add(new AuditLog
            {
                Action = action, EntityType = entityType, EntityId = entityId, Details = details,
                UserId = userId ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier),
                UserName = userName ?? principal?.Identity?.Name,
                IpAddress = http?.Connection.RemoteIpAddress?.ToString()
            });
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write audit event {Action}", action);
        }
    }
}
