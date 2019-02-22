using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace QueueT.Tasks
{

    public interface ITaskService
    {
        ITaskRegistry Registry { get; }

        Task<TaskMessage> DelayAsync<T>(Expression<Action<T>> expression, DispatchOptions options = null);

        Task<TaskMessage> DelayAsync<T>(Expression<Func<T, Task>> expression, DispatchOptions options = null);

        Task<TaskMessage> DelayAsync(MethodInfo methodInfo, object arguments, DispatchOptions options = null);

        Task<TaskMessage> DelayAsync(string TaskName, object arguments, DispatchOptions options = null);

        Task<object> ExecuteTaskMessageAsync(TaskMessage message);
    }
}