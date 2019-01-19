using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace QueueT.Tasks
{
    public interface IQueueTTaskService
    {
        Task<TaskMessage> DispatchAsync(MethodInfo methodInfo, IDictionary<string, object> arguments);

        Task<object> ExecuteTaskMessageAsync(TaskMessage message);
    }
}