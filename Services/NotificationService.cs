using Microsoft.AspNetCore.Components;

namespace BlazorQuizApp.Services
{
    public enum NotificationType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public class Notification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsVisible { get; set; } = true;
        public int Duration { get; set; } = 5000; // 5 seconds default
    }

    public interface INotificationService
    {
        event Action<List<Notification>>? OnNotificationsChanged;
        List<Notification> Notifications { get; }
        void ShowSuccess(string message, int duration = 5000);
        void ShowError(string message, int duration = 8000);
        void ShowWarning(string message, int duration = 6000);
        void ShowInfo(string message, int duration = 5000);
        void RemoveNotification(string id);
        void ClearAll();
    }

    public class NotificationService : INotificationService
    {
        private readonly List<Notification> _notifications = new();
        private readonly Timer _cleanupTimer;

        public event Action<List<Notification>>? OnNotificationsChanged;

        public List<Notification> Notifications => _notifications.ToList();

        public NotificationService()
        {
            _cleanupTimer = new Timer(CleanupExpiredNotifications, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public void ShowSuccess(string message, int duration = 5000)
        {
            AddNotification(message, NotificationType.Success, duration);
        }

        public void ShowError(string message, int duration = 8000)
        {
            AddNotification(message, NotificationType.Error, duration);
        }

        public void ShowWarning(string message, int duration = 6000)
        {
            AddNotification(message, NotificationType.Warning, duration);
        }

        public void ShowInfo(string message, int duration = 5000)
        {
            AddNotification(message, NotificationType.Info, duration);
        }

        public void RemoveNotification(string id)
        {
            var notification = _notifications.FirstOrDefault(n => n.Id == id);
            if (notification != null)
            {
                notification.IsVisible = false;
                _notifications.Remove(notification);
                OnNotificationsChanged?.Invoke(_notifications);
            }
        }

        public void ClearAll()
        {
            _notifications.Clear();
            OnNotificationsChanged?.Invoke(_notifications);
        }

        private void AddNotification(string message, NotificationType type, int duration)
        {
            var notification = new Notification
            {
                Message = message,
                Type = type,
                Duration = duration
            };

            _notifications.Add(notification);
            OnNotificationsChanged?.Invoke(_notifications);
        }

        private void CleanupExpiredNotifications(object? state)
        {
            var now = DateTime.UtcNow;
            var expiredNotifications = _notifications
                .Where(n => now.Subtract(n.CreatedAt).TotalMilliseconds > n.Duration)
                .ToList();

            foreach (var notification in expiredNotifications)
            {
                _notifications.Remove(notification);
            }

            if (expiredNotifications.Any())
            {
                OnNotificationsChanged?.Invoke(_notifications);
            }
        }
    }
} 