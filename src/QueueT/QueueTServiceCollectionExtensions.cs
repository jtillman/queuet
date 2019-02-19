using Microsoft.Extensions.DependencyInjection;
using System;
using QueueT.Worker;


namespace QueueT
{
    public static class QueueTServiceCollectionExtensions
    {
        public static QueueTServiceCollection AddQueueT(this IServiceCollection serviceCollection, Action<QueueTServiceOptions> configure = null)
        {
            serviceCollection.AddTransient<IMessageDispatcher, MessageDispatcher>();

            serviceCollection.AddSingleton<Notifications.INotificationRegistry, Notifications.NotificationRegistry>();
            serviceCollection.AddScoped<Notifications.INotificationDispatcher, Notifications.NotificationDispatcher>();

            if (null != configure)
                serviceCollection.Configure<QueueTServiceOptions>(options=> {
                    options.RegisterHandlerType<Notifications.NotificationMessageHandler>();
                    configure(options);
                });
            return new QueueTServiceCollection(serviceCollection);
        }

        public static IServiceCollection AddQueueTWorker(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddHostedService<QueueTWorkerService>();
            return serviceCollection;
        }
    }
}
