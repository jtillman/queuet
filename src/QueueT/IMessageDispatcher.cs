using System.Threading.Tasks;

namespace QueueT
{
    public interface IMessageDispatcher
    {
        Task<QueueTMessage> SendMessageAsync(string messageType, object message, DispatchOptions dispatchOptions = null);
    }
}
