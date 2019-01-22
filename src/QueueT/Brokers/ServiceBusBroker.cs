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

        private string _connectionString;

        public ServiceBusBroker(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public QueueClient GetOrCreateQueueClient(string queueName)
        {
            if (!_senderQueueClients.TryGetValue(queueName, out var queueClient))
            {
                queueClient = new QueueClient(_connectionString, queueName);
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
