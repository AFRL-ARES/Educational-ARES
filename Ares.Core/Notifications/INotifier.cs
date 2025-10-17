namespace Ares.Core.Notifications;

public interface INotifier
{
  Task Notify(string message, string title, NotificationSeverityEnum severity);
}
