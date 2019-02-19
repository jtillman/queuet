using System;

namespace QueueT.Notifications
{

    [AttributeUsage(AttributeTargets.Field)]
    public class TopicAttribute : Attribute
    {
        public string Template { get; set; }

        public Type MessageType { get; set; }

        public TopicAttribute() {}

        public TopicAttribute(string template)
        {
            Template = template;
        }
        public TopicAttribute(string template, Type messageType)
        {
            Template = template;
            MessageType = messageType;
        }
    }
}
