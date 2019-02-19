using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace QueueT.Notifications
{
    public class NotificationDispatcher : INotificationDispatcher
    {
        private readonly ILogger<NotificationDispatcher> _logger;

        private readonly INotificationRegistry _notificationRegistry;

        private readonly IMessageDispatcher _messageDispatcher;


        public NotificationDispatcher(
            ILogger<NotificationDispatcher> logger,
            INotificationRegistry notificationRegistry,
            IMessageDispatcher messageDispatcher)
        {
            _logger = logger;
            _messageDispatcher = messageDispatcher;
            _notificationRegistry = notificationRegistry;
        }

        // Subscriptions
        public async Task NotifyAsync(Enum notificationEnum, object value, DispatchOptions options = null)
        {
            var notificationDefinition = _notificationRegistry.GetNotificationByEnum(notificationEnum);

            options = options ?? new DispatchOptions();
            options.Properties[NotificationMessage.TopicPropertyKey] = notificationDefinition.Topic;

            await _messageDispatcher.SendMessageAsync(NotificationMessage.MessageType, value, options);
        }

        public async Task NotifyAsync<T>(Enum topicEnumValue, T value, DispatchOptions dispatchOptions = null) => await NotifyAsync(topicEnumValue, value as object, dispatchOptions);
    }
}
