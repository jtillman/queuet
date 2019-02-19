using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace QueueT.Tasks
{
    public class TaskRegistry : ITaskRegistry
    {
        public ILogger<TaskRegistry> _logger;

        private IDictionary<string, TaskDefinition> TaskDefinitionsByName { get; }
            = new Dictionary<string, TaskDefinition>();

        private IDictionary<MethodInfo, TaskDefinition> TaskDefinitionsByMethod { get; }
            = new Dictionary<MethodInfo, TaskDefinition>();

        public TaskRegistry(ILogger<TaskRegistry> logger, IOptions<TaskServiceOptions> options)
        {
            _logger = logger;

            foreach(var taskDefinition in options.Value.Tasks)
            {
                AddTask(taskDefinition);
            }
        }

        public void AddTask(TaskDefinition taskDefinition)
        {
            if (taskDefinition == null)
                throw new ArgumentNullException(nameof(taskDefinition));

            if (TaskDefinitionsByName.ContainsKey(taskDefinition.Name))
                throw new ArgumentException($"Attempting to add task with duplcate name [{taskDefinition.Name}]");

            if (TaskDefinitionsByMethod.ContainsKey(taskDefinition.Method))
                throw new ArgumentException($"Task for method is already registered: {taskDefinition.Method}");

            _logger.LogInformation($"Registering task: {taskDefinition.Name}");

            TaskDefinitionsByName.Add(taskDefinition.Name, taskDefinition);
            TaskDefinitionsByMethod.Add(taskDefinition.Method, taskDefinition);
        }

        public TaskDefinition GetTaskByName(string taskName)
        {
            if (TaskDefinitionsByName.TryGetValue(taskName, out var definition))
                return definition;
            return null;
        }

        public TaskDefinition GetTaskByMethod(MethodInfo methodInfo)
        {
            if (TaskDefinitionsByMethod.TryGetValue(methodInfo, out var definition))
                return definition;
            return null;
        }
    }
}
