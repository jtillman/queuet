using System;
using System.Reflection;

namespace QueueT.Notifications
{
    public class NotificationSubscription : MethodHandler
    {
        public NotificationDefinition Notification { get; }

        public string TaskQueue { get; }

        public NotificationSubscription(
            NotificationDefinition notification, 
            MethodInfo method, 
            string taskQueue = null) : base(method)
        {
            Notification = notification ?? throw new ArgumentNullException(nameof(notification));
            TaskQueue = taskQueue;
        }
    }
}
