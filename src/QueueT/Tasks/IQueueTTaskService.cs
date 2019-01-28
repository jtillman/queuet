using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace QueueT.Tasks
{

    public interface IQueueTTaskService
    {
        Task<TaskMessage> DelayAsync<T>(Expression<Action<T>> expression);

        Task<TaskMessage> DelayAsync<T>(Expression<Func<T, Task>> expression);

        Task<TaskMessage> DelayAsync(MethodInfo methodInfo, IDictionary<string, object> arguments);

        Task<object> ExecuteTaskMessageAsync(TaskMessage message);
    }
}