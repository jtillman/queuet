using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace QueueT.Tasks
{

    public interface ITaskService
    {
        Task<TaskMessage> DelayAsync<T>(Expression<Action<T>> expression, TaskDispatchOptions options = null);

        Task<TaskMessage> DelayAsync<T>(Expression<Func<T, Task>> expression, TaskDispatchOptions options = null);

        Task<TaskMessage> DelayAsync(MethodInfo methodInfo, IDictionary<string, object> arguments, TaskDispatchOptions options = null);

        Task<string> DispatchAsync(TaskDefinition definition, byte[] encodedArguments, TaskDispatchOptions options = null);

        Task<object> ExecuteTaskMessageAsync(TaskMessage message);
    }
}