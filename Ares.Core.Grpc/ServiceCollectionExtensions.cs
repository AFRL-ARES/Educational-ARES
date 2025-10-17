using Ares.Core.Grpc.Services.Notifications;
using Ares.Core.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace Ares.Core.Grpc;

public static class ServiceCollectionExtensions
{
  public static void AddNotificationHandlers(this IServiceCollection services)
  {
    services.AddSingleton<IAresNotificationRepo, AresNotificationRepo>();
    var notificationService = new AresNotificationService(new AresNotificationRepo());
    var handler = new NotificationHandler(notificationService);
    services.AddSingleton<INotificationHandler>(handler);

  }
}