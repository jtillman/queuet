using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QueueT.Brokers
{
    public class InMemoryBroker : IQueueTBroker
    {
        public ConcurrentDictionary<string, Queue<QueueTMessage>> Queues =
            new ConcurrentDictionary<string, Queue<QueueTMessage>>();

        public InMemoryBroker(params string[] queueNames)
        {
            foreach(var queueName in queueNames)
            {
                Queues[queueName] = new Queue<QueueTMessage>();
            }
        }

        public async Task SendAsync(string queueName, QueueTMessage message)
        {
            if (!Queues.TryGetValue(queueName, out var queue))
            {
                queue = new Queue<QueueTMessage>();
                Queues[queueName] = queue;
            }
            queue.Enqueue(message);
            await Task.CompletedTask;
        }

        public async Task<int> ReceiveMessagesAsync(
            string queueName,
            int maxMessages,
            Func<QueueTMessage, CancellationToken, Task> messageProcessor,
            CancellationToken cancellationToken)
        {
        var messagesRead = 0;

            if (!Queues.TryGetValue(queueName, out var queue))
            {
                return 0;
            }

            while (!cancellationToken.IsCancellationRequested && queue.Count > 0 && messagesRead < maxMessages)
            {
                var message = queue.Dequeue();

                try
                {
                    await messageProcessor(message, cancellationToken);
                }catch (MessageProcessingException processingException) {

                    switch (processingException.Action)
                    {
                        case MessageAction.Retry:
                            queue.Enqueue(message);
                            break;
                    }
                }
            }

            return await Task.FromResult(messagesRead);
        }
    }
}
