using System;
using System.Threading.Tasks;

namespace QueueT.Notifications
{
    public interface INotificationService
    {
        Task NotifyAsync(Enum notificationEnum, object value, DispatchOptions options = null);
        Task NotifyAsync<T>(Enum topicEnumValue, T value, DispatchOptions dispatchOptions = null);
    }
}