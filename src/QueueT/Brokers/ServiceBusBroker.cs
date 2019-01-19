using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QueueT.Brokers
{

    public class ServiceBusBroker : IQueueTBroker
    {
        // The type of queuet message was sent
        public const string MessageTypeProperty = "message-type";

        private Dictionary<string, QueueClient> _senderQueueClients =
            new Dictionary<string, QueueClient>();

        private ServiceBusConnection _connection;

        private RetryPolicy _retryPolicy;

        public ServiceBusBroker(ServiceBusConnection connection, RetryPolicy retryPolicy = null)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _retryPolicy = retryPolicy ?? RetryPolicy.Default;
        }

        public QueueClient GetOrCreateQueueClient(string queueName)
        {
            if (!_senderQueueClients.TryGetValue(queueName, out var queueClient))
            {
                queueClient = new QueueClient(_connection, queueName, ReceiveMode.PeekLock, _retryPolicy);
                _senderQueueClients[queueName] = queueClient;
            }
            return queueClient;
        }

        public async Task SendAsync(string queueName, QueueTMessage message)
        {
            var queueClient = GetOrCreateQueueClient(queueName);

            var serviceBusMessage = message.ToServiceBusMessage();

            await queueClient.SendAsync(serviceBusMessage);
        }
    }

    public static class ServiceBusExtensions
    {
        public static QueueTMessage ToQueueTMessage(this Message message)
        {
            var properties = new Dictionary<string, string>();
            var queueTMessage = new QueueTMessage
            {
                Id = message.MessageId,
                ContentType = message.ContentType,
            };

            queueTMessage.MessageType = message.UserProperties[ServiceBusBroker.MessageTypeProperty] as string;

            foreach (var property in message.UserProperties)
            {
                if (message.UserProperties[property.Key] is string value)
                    queueTMessage.Properties[property.Key] = value;
            }

            return queueTMessage;
        }

        public static Message ToServiceBusMessage(this QueueTMessage message)
        {
            var serviceBusMessage = new Message(message.Message)
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
