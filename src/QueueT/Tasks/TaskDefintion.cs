using System.Reflection;

namespace QueueT.Tasks
{
    public class TaskDefinition
    {
        public string TaskName { get; }

        public MethodInfo Method { get; }

        public string QueueName { get; }

        public ParameterInfo[] Parameters { get; }

        public TaskDefinition(string taskName, MethodInfo method, string queueName)
        {
            TaskName = taskName;
            Method = method;
            QueueName = queueName;
            Parameters = method.GetParameters();
        }
    }
}
