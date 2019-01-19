using System.Threading.Tasks;

namespace QueueT
{
    public interface IQueueTBroker
    {
        Task SendAsync(string queueName, QueueTMessage message);
        // Send Message to Queue
        // StartReceiving Message From Queue
        // StopReceiving From Queue
    }
}
