using System.Collections.Generic;

namespace QueueT.Notifications
{
    public class NotificationOptions
    {
        public string DefaultQueueName { get; set; }

        public List<NotificationDefinition> Notifications { get; set; }
            = new List<NotificationDefinition>();

        public List<NotificationSubscription> NotificationSubscriptions { get; set; }
            = new List<NotificationSubscription>();
    }
}
