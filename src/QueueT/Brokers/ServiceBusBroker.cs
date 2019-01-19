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
}
