using System;
using System.Collections.Generic;
using System.Text;

namespace QueueT
{
    public class QueueTServiceOptions
    {
        public string DefaultQueueName { get; set; } = "default";

        public IList<string> Queues { get; set; } = new List<string>();

        public int WorkerBatchSize { get; set; } = 10;

        public IQueueTBroker Broker { get; set; }

        public IList<Type> MessageHandlerTypes { get; set; } = new List<Type>();

        public void RegisterHandlerType<T>() where T: IQueueTMessageHandler
        {
            MessageHandlerTypes.Add(typeof(T));
        }

        public void AddQueues(params string[] queues)
        {
            foreach (var queue in queues) { Queues.Add(queue); }
        }
    }
}