using System;
using System.Threading;
using System.Threading.Tasks;

namespace QueueT
{
    public interface IQueueTBroker
    {
        Task SendAsync(string queueName, QueueTMessage message);

        Task<int> ReceiveMessagesAsync(
            string queueName,
            int maxMessages,
            Func<QueueTMessage, CancellationToken, Task> messageProcessor,
            CancellationToken cancellationToken);
    }
}
