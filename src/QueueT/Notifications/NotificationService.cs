using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace QueueT.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;

        private readonly NotificationOptions _options;

        private readonly INotificationRegistry _notificationRegistry;

        private readonly IMessageDispatcher _messageDispatcher;


        public NotificationService(
            ILogger<NotificationService> logger,
            IOptions<NotificationOptions> options,
            INotificationRegistry notificationRegistry,
            IMessageDispatcher messageDispatcher)
        {
            _logger = logger;
            _options = options.Value;
            _messageDispatcher = messageDispatcher;
            _notificationRegistry = notificationRegistry;
        }

        // Subscriptions
        public async Task NotifyAsync(Enum notificationEnum, object value, DispatchOptions options = null)
        {
            var notificationDefinition = _notificationRegistry.GetNotificationByEnum(notificationEnum);

            options = options ?? new DispatchOptions();
            options.Properties[NotificationMessage.TopicPropertyKey] = notificationDefinition.Topic;
            options.Queue = options.Queue ?? _options.DefaultQueueName;
            await _messageDispatcher.SendMessageAsync(NotificationMessage.MessageType, value, options);
        }

        public async Task NotifyAsync<T>(Enum topicEnumValue, T value, DispatchOptions dispatchOptions = null) => await NotifyAsync(topicEnumValue, value as object, dispatchOptions);
    }
}
