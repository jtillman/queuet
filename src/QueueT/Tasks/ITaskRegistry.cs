using System.Reflection;

namespace QueueT.Tasks
{
    public interface ITaskRegistry
    {
        void AddTask(TaskDefinition taskDefinition);
        TaskDefinition GetTaskByName(string taskName);
        TaskDefinition GetTaskByMethod(MethodInfo methodInfo);
    }
}
