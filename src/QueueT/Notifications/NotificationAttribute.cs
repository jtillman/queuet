using System;

namespace QueueT.Notifications
{
    [AttributeUsage(AttributeTargets.Field)]
    public class NotificationAttribute : Attribute
    {
        public string Topic { get; set; }

        public Type MessageType { get; set; }

        public NotificationAttribute(string topic, Type messageType)
        {
            Topic = topic;
            MessageType = messageType;
        }
    }
}
