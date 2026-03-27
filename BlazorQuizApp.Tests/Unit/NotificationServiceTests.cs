using BlazorQuizApp.Services;
using Xunit;

namespace BlazorQuizApp.Tests.Unit
{
    public class NotificationServiceTests : IDisposable
    {
        private readonly NotificationService _sut = new();

        // ── ShowSuccess / ShowError / ShowWarning / ShowInfo ────────────────────

        [Fact]
        public void ShowSuccess_AddsNotificationWithCorrectType()
        {
            _sut.ShowSuccess("Operation succeeded");

            var n = Assert.Single(_sut.Notifications);
            Assert.Equal("Operation succeeded", n.Message);
            Assert.Equal(NotificationType.Success, n.Type);
        }

        [Fact]
        public void ShowError_AddsNotificationWithCorrectTypeAndLongerDuration()
        {
            _sut.ShowError("Something went wrong");

            var n = Assert.Single(_sut.Notifications);
            Assert.Equal(NotificationType.Error, n.Type);
            Assert.Equal(8000, n.Duration);
        }

        [Fact]
        public void ShowWarning_AddsNotificationWithCorrectType()
        {
            _sut.ShowWarning("Watch out");

            var n = Assert.Single(_sut.Notifications);
            Assert.Equal(NotificationType.Warning, n.Type);
            Assert.Equal(6000, n.Duration);
        }

        [Fact]
        public void ShowInfo_AddsNotificationWithCorrectType()
        {
            _sut.ShowInfo("FYI");

            var n = Assert.Single(_sut.Notifications);
            Assert.Equal(NotificationType.Info, n.Type);
        }

        [Fact]
        public void Show_CustomDuration_AppliedCorrectly()
        {
            _sut.ShowSuccess("msg", duration: 1234);
            Assert.Equal(1234, _sut.Notifications[0].Duration);
        }

        // ── Multiple notifications ──────────────────────────────────────────────

        [Fact]
        public void MultipleShow_AllStoredInOrder()
        {
            _sut.ShowSuccess("first");
            _sut.ShowError("second");
            _sut.ShowInfo("third");

            var list = _sut.Notifications;
            Assert.Equal(3, list.Count);
            Assert.Equal("first", list[0].Message);
            Assert.Equal("second", list[1].Message);
            Assert.Equal("third", list[2].Message);
        }

        // ── Notifications property returns snapshot ─────────────────────────────

        [Fact]
        public void Notifications_ReturnsSnapshot_NotLiveList()
        {
            _sut.ShowSuccess("one");
            var snapshot = _sut.Notifications;
            _sut.ShowSuccess("two");

            // snapshot captured before "two" was added
            Assert.Single(snapshot);
            Assert.Equal(2, _sut.Notifications.Count);
        }

        // ── IsVisible & Id defaults ─────────────────────────────────────────────

        [Fact]
        public void NewNotification_IsVisibleAndHasId()
        {
            _sut.ShowInfo("hello");

            var n = _sut.Notifications[0];
            Assert.True(n.IsVisible);
            Assert.False(string.IsNullOrEmpty(n.Id));
        }

        // ── RemoveNotification ──────────────────────────────────────────────────

        [Fact]
        public void RemoveNotification_ExistingId_RemovesIt()
        {
            _sut.ShowSuccess("to remove");
            var id = _sut.Notifications[0].Id;

            _sut.RemoveNotification(id);

            Assert.Empty(_sut.Notifications);
        }

        [Fact]
        public void RemoveNotification_NonExistentId_NoError()
        {
            _sut.ShowSuccess("keep");
            _sut.RemoveNotification("unknown-id");

            Assert.Single(_sut.Notifications); // original unchanged
        }

        [Fact]
        public void RemoveNotification_OnlyRemovesTargetNotification()
        {
            _sut.ShowSuccess("keep");
            _sut.ShowError("remove me");

            var removeId = _sut.Notifications[1].Id;
            _sut.RemoveNotification(removeId);

            var remaining = _sut.Notifications;
            Assert.Single(remaining);
            Assert.Equal("keep", remaining[0].Message);
        }

        // ── ClearAll ────────────────────────────────────────────────────────────

        [Fact]
        public void ClearAll_RemovesAllNotifications()
        {
            _sut.ShowSuccess("a");
            _sut.ShowError("b");
            _sut.ShowWarning("c");

            _sut.ClearAll();

            Assert.Empty(_sut.Notifications);
        }

        [Fact]
        public void ClearAll_WhenEmpty_NoError()
        {
            var ex = Record.Exception(() => _sut.ClearAll());
            Assert.Null(ex);
        }

        // ── OnNotificationsChanged event ────────────────────────────────────────

        [Fact]
        public void Show_RaisesOnNotificationsChangedEvent()
        {
            List<Notification>? received = null;
            _sut.OnNotificationsChanged += list => received = list;

            _sut.ShowSuccess("event test");

            Assert.NotNull(received);
            Assert.Single(received!);
        }

        [Fact]
        public void ClearAll_RaisesOnNotificationsChangedWithEmptyList()
        {
            _sut.ShowSuccess("item");
            List<Notification>? received = null;
            _sut.OnNotificationsChanged += list => received = list;

            _sut.ClearAll();

            Assert.NotNull(received);
            Assert.Empty(received!);
        }

        [Fact]
        public void RemoveNotification_RaisesOnNotificationsChangedEvent()
        {
            _sut.ShowInfo("msg");
            var id = _sut.Notifications[0].Id;
            List<Notification>? received = null;
            _sut.OnNotificationsChanged += list => received = list;

            _sut.RemoveNotification(id);

            Assert.NotNull(received);
        }

        public void Dispose()
        {
            // NotificationService spins up a cleanup Timer; allow GC to collect it.
            // In production it would be registered as IDisposable via DI.
        }
    }
}
