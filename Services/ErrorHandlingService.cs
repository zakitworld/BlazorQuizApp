using Microsoft.Extensions.Logging;

namespace BlazorQuizApp.Services
{
    /// <summary>
    /// Service for centralized error handling and logging
    /// </summary>
    public interface IErrorHandlingService
    {
        Task<T?> ExecuteWithErrorHandlingAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            T? defaultValue = default) where T : class;

        Task<bool> ExecuteWithErrorHandlingAsync(
            Func<Task> operation,
            string operationName);

        void LogError(Exception ex, string message, params object[] args);
        void LogWarning(string message, params object[] args);
    }

    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;

        public ErrorHandlingService(ILogger<ErrorHandlingService> logger)
        {
            _logger = logger;
        }

        public async Task<T?> ExecuteWithErrorHandlingAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            T? defaultValue = default) where T : class
        {
            try
            {
                _logger.LogDebug("Executing operation: {OperationName}", operationName);
                return await operation();
            }
            catch (Exception ex)
            {
                LogError(ex, "Error executing operation: {OperationName}", operationName);
                return defaultValue;
            }
        }

        public async Task<bool> ExecuteWithErrorHandlingAsync(
            Func<Task> operation,
            string operationName)
        {
            try
            {
                _logger.LogDebug("Executing operation: {OperationName}", operationName);
                await operation();
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error executing operation: {OperationName}", operationName);
                return false;
            }
        }

        public void LogError(Exception ex, string message, params object[] args)
        {
            _logger.LogError(ex, message, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }
    }
}

