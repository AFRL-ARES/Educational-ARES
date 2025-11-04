namespace Ares.Core.Notifications;

public interface INotifier
{
  Task Notify(string title, string message, NotificationSeverityEnum severity);
}
