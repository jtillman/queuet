using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QueueT.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QueueT.Notifications
{

    public class NotificationRegistry : INotificationRegistry
    {
        private ILogger<NotificationRegistry> _logger;

        private ITaskRegistry _taskRegistry;

        private Dictionary<NotificationDefinition, List<NotificationSubscription>> NotificationSubscriptions { get; }
            = new Dictionary<NotificationDefinition, List<NotificationSubscription>>();

        private Dictionary<string, NotificationDefinition> _notificationsByTopic { get; }
            = new Dictionary<string, NotificationDefinition>();

        private Dictionary<Enum, NotificationDefinition> _notificationsByEnum { get; }
            = new Dictionary<Enum, NotificationDefinition>();

        public NotificationRegistry(
            ILogger<NotificationRegistry> logger,
            ITaskRegistry taskRegistry,
            IOptions<NotificationOptions> options)
        {
            _logger = logger;
            _taskRegistry = taskRegistry;

            foreach (var notifications in options.Value.Notifications)
            {
                RegisteryNotification(notifications);
            }

            foreach(var subscription in options.Value.NotificationSubscriptions)
            {
                RegisterSubscription(subscription);
            }

        }

        public void RegisteryNotification(NotificationDefinition notificationDefinition)
        {
            if (notificationDefinition == null)
            {
                throw new ArgumentNullException(nameof(notificationDefinition));
            }

            if (_notificationsByTopic.ContainsKey(notificationDefinition.Topic))
            {
                throw new ArgumentException($"Notification already registered for topic: {notificationDefinition.Topic}");
            }

            var hasEnum = null != notificationDefinition.EnumValue;
            if (hasEnum &&_notificationsByEnum.ContainsKey(notificationDefinition.EnumValue))
            {
                throw new ArgumentException($"Notification already registered for enum: {notificationDefinition.EnumValue}");
            }

            _notificationsByTopic[notificationDefinition.Topic] = notificationDefinition;

            if (hasEnum)
            {
                _notificationsByEnum[notificationDefinition.EnumValue] = notificationDefinition;
            }
        }


        public NotificationDefinition GetNotificationByEnum(Enum notificationEnum)
        {
            if (!_notificationsByEnum.TryGetValue(notificationEnum, out var definition)) {
                throw new ArgumentException($"No notification present for enum {notificationEnum}");
            }
            return definition;
        }

        public NotificationDefinition GetNotificationByTopic(string topic)
        {
            if (!_notificationsByTopic.TryGetValue(topic, out var definition))
            {
                throw new ArgumentException($"No notification present for topic {topic}");
            }
            return definition;
        }

        public void RegisterSubscription(NotificationSubscription subscription)
        {
            if (!NotificationSubscriptions.TryGetValue(subscription.Notification, out var subscriptions))
            {
                subscriptions = new List<NotificationSubscription>();
                NotificationSubscriptions[subscription.Notification] = subscriptions;
            }

            if (subscriptions.Any(s => s.Notification == subscription.Notification && s.Method == subscription.Method))
            {
                throw new ArgumentException($"Already found subscription for topic to method Topic={subscription.Notification.Topic} Method={subscription.Method}");
            }

            if (null == _taskRegistry.GetTaskByMethod(subscription.Method))
            {
                throw new ArgumentException($"Method {subscription.Method} must be registered as task to accept notifications");
            }

            NotificationSubscriptions[subscription.Notification].Add(subscription);
        }

        public NotificationSubscription[] GetSubscriptions(NotificationDefinition notificationDefinition)
        {
            if (!NotificationSubscriptions.TryGetValue(notificationDefinition, out var methodHandlers))
                return new NotificationSubscription[] { };
            return methodHandlers.ToArray();
        }
    }
}
