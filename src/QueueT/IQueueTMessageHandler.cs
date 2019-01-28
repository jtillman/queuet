using System.Threading.Tasks;

namespace QueueT
{
    public interface IQueueTMessageHandler
    {
        bool CanHandleMessage(QueueTMessage message);
        Task HandleMessage(QueueTMessage message);
    }
}