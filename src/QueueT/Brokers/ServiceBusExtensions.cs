using Microsoft.Azure.ServiceBus;
using System.Collections.Generic;

namespace QueueT.Brokers
{
    public static class ServiceBusExtensions
    {
        public static QueueTMessage ToQueueTMessage(this Message message)
        {
            var properties = new Dictionary<string, string>();
            var queueTMessage = new QueueTMessage
            {
                Id = message.MessageId,
                ContentType = message.ContentType,
                EncodedBody = message.Body,
                Properties = new Dictionary<string, string>()
            };

            queueTMessage.MessageType = message.UserProperties[ServiceBusBroker.MessageTypeProperty] as string;
            message.UserProperties.Remove(ServiceBusBroker.MessageTypeProperty);

            foreach (var property in message.UserProperties)
            {
                if (message.UserProperties[property.Key] is string value)
                    queueTMessage.Properties[property.Key] = value;
            }

            return queueTMessage;
        }

        public static Message ToServiceBusMessage(this QueueTMessage message)
        {
            var serviceBusMessage = new Message(message.EncodedBody)
            {
                MessageId = message.Id,
                ContentType = message.ContentType
            };

            foreach (var property in message.Properties)
                serviceBusMessage.UserProperties[property.Key] = property.Value;

            serviceBusMessage.UserProperties[ServiceBusBroker.MessageTypeProperty] = message.MessageType;

            return serviceBusMessage;
        }
    }
}
