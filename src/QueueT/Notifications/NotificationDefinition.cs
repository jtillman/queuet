using System;

namespace QueueT.Notifications
{
    public class NotificationDefinition
    {
        public string Topic { get; }

        public Type BodyType { get; }

        public Enum EnumValue { get; }

        public NotificationDefinition(string topic, Type bodyType, Enum enumValue)
        {
            Topic = topic ?? throw new ArgumentNullException(nameof(topic));
            BodyType = bodyType ?? throw new ArgumentNullException(nameof(bodyType));
            EnumValue = enumValue;
        }
    }
}
