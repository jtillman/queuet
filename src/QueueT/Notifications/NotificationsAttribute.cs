using System;

namespace QueueT.Notifications
{
    [AttributeUsage(AttributeTargets.Enum)]
    public class NotificationsAttribute : Attribute
    {
        public string TopicTemplate { get; set; }

        public Type DefaultMessageType { get; set; }

        public NotificationsAttribute() { }

        public NotificationsAttribute(string topicTemplate) { TopicTemplate = topicTemplate; }

        public NotificationsAttribute(string topicTemplate, Type defaultMessageType) 
        {
            TopicTemplate = topicTemplate;
            DefaultMessageType = defaultMessageType;
        }
    }
}
