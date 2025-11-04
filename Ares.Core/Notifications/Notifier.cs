namespace Ares.Core.Notifications;

public class Notifier : INotifier
{
  private readonly IList<INotificationHandler> _notificationHandlers;
  public Notifier(IEnumerable<INotificationHandler> notificationHandlers)
  {
    _notificationHandlers = notificationHandlers.ToList();
  }

  public async Task Notify(string title, string message, NotificationSeverityEnum severity)
  {
    foreach(var handler in _notificationHandlers)
    {
      await handler.HandleNotification(title, message, severity);
    }
  }
}
