using System.Collections.Generic;
using System.Threading.Tasks;

namespace QueueT.Brokers
{
    public class InMemoryBroker : IQueueTBroker
    {
        Dictionary<string, Queue<QueueTMessage>> _queues =
            new Dictionary<string, Queue<QueueTMessage>>();

        public async Task SendAsync(string queueName, QueueTMessage message)
        {
            if(!_queues.TryGetValue(queueName, out var queue))
            {
                queue = new Queue<QueueTMessage>();
                _queues[queueName] = queue;
            }
            queue.Enqueue(message);
            await Task.CompletedTask;
        }
    }
}
