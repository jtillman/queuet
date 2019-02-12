using System.Threading.Tasks;

namespace QueueT
{
    public interface IMessageHandler
    {
        bool CanHandleMessage(QueueTMessage message);
        Task HandleMessage(QueueTMessage message);
    }
}