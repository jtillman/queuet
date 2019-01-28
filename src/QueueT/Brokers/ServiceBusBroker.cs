using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QueueT.Brokers
{

    public class ServiceBusBroker : IQueueTBroker
    {
        // The type of queuet message was sent
        public const string MessageTypeProperty = "message-type";

        private Dictionary<string, QueueClient> _senderQueueClients =
            new Dictionary<string, QueueClient>();

        private Dictionary<string, MessageReceiver> _receiverQueueClients =
            new Dictionary<string, MessageReceiver>();

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

        public MessageReceiver GetOrCreateReceiverQueueClient(string queueName)
        {
            if (!_receiverQueueClients.TryGetValue(queueName, out var queueClient))
            {
                queueClient = new MessageReceiver(_connectionString, queueName);
                _receiverQueueClients[queueName] = queueClient;
            }
            return queueClient;
        }

        public async Task SendAsync(string queueName, QueueTMessage message)
        {
            var queueClient = GetOrCreateQueueClient(queueName);

            var serviceBusMessage = message.ToServiceBusMessage();

            await queueClient.SendAsync(serviceBusMessage);
        }

        public async Task<int> ReceiveMessagesAsync(
            string queueName,
            int maxMessages,
            Func<QueueTMessage, CancellationToken, Task> messageProcessor,
            CancellationToken cancellationToken)
        {
            var messagesProcessed = 0;
            var receiver = GetOrCreateReceiverQueueClient(queueName);

            while(!cancellationToken.IsCancellationRequested && maxMessages > messagesProcessed)
            {
                var messages = await receiver.ReceiveAsync(maxMessages - messagesProcessed);
                if (messages.Count == 0) break;

                foreach(var message in messages)
                {
                    try
                    {
                        await messageProcessor(message.ToQueueTMessage(), cancellationToken);
                        await receiver.CompleteAsync(message.SystemProperties.LockToken);

                    } catch (MessageProcessingException processingException){

                        switch (processingException.Action)
                        {
                            case MessageAction.Retry:
                                await receiver.DeferAsync(message.SystemProperties.LockToken);
                                break;
                            default:
                                await receiver.DeadLetterAsync(message.SystemProperties.LockToken);
                                break;
                        }
                    }
                    finally { messagesProcessed++; }
                }
            }
            return messagesProcessed;
        }
    }
}
