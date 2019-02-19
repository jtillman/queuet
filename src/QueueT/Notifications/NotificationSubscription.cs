using System;
using System.Linq;
using System.Reflection;

namespace QueueT.Notifications
{
    public class NotificationSubscription : MethodHandler
    {
        public NotificationDefinition Notification { get; }

        public ParameterInfo MessageParameter { get; }

        public string TaskQueue { get; }

        public NotificationSubscription(
            NotificationDefinition notification, 
            MethodInfo method,
            string messageParameterName,
            string taskQueue = null) : base(method)
        {
            Notification = notification ?? throw new ArgumentNullException(nameof(notification));
            MessageParameter = Parameters.FirstOrDefault(x => x.Name == messageParameterName) ?? throw new ArgumentException($"No Parameter {messageParameterName} on {method}");
            TaskQueue = taskQueue;
        }
    }
}
