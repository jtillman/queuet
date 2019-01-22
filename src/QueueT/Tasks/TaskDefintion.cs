using System.Reflection;

namespace QueueT.Tasks
{
    public class TaskDefinition
    {
        public string Name { get; }

        public MethodInfo Method { get; }

        public string QueueName { get; }

        public ParameterInfo[] Parameters { get; }

        public TaskDefinition(string taskName, MethodInfo method, string queueName)
        {
            if (string.IsNullOrWhiteSpace(taskName))
            {
                throw new System.ArgumentException("message", nameof(taskName));
            }

            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new System.ArgumentException("message", nameof(queueName));
            }

            Name = taskName.Trim();
            Method = method ?? throw new System.ArgumentNullException(nameof(method));
            QueueName = queueName.Trim();
            Parameters = method.GetParameters();
        }
    }
}
