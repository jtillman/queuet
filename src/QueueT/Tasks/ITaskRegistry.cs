using System.Collections.Generic;
using System.Reflection;

namespace QueueT.Tasks
{
    public interface ITaskRegistry
    {
        IEnumerable<TaskDefinition> TaskDefinitions { get; }
        void AddTask(TaskDefinition taskDefinition);
        TaskDefinition GetTaskByName(string taskName);
        TaskDefinition GetTaskByMethod(MethodInfo methodInfo);
    }
}
