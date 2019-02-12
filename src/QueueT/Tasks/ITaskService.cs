using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace QueueT.Tasks
{

    public interface ITaskService
    {
        Task<TaskMessage> DelayAsync<T>(Expression<Action<T>> expression, DispatchOptions options = null);

        Task<TaskMessage> DelayAsync<T>(Expression<Func<T, Task>> expression, DispatchOptions options = null);

        Task<TaskMessage> DelayAsync(MethodInfo methodInfo, IDictionary<string, object> arguments, DispatchOptions options = null);

        Task<string> DispatchAsync(TaskDefinition definition, byte[] encodedArguments, DispatchOptions options = null);

        Task<object> ExecuteTaskMessageAsync(TaskMessage message);
    }
}