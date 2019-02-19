using System;

namespace QueueT.Notifications
{
    public interface INotificationRegistry
    {
        void RegisteryNotification(NotificationDefinition notificationDefinition);

        NotificationDefinition GetNotificationByEnum(Enum notificationEnum);

        NotificationDefinition GetNotificationByTopic(string topic);

        void RegisterSubscription(NotificationSubscription subscription);

        NotificationSubscription[] GetSubscriptions(NotificationDefinition notificationDefinition);
    }
}
