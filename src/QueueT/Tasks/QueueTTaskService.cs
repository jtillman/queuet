using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QueueT.Tasks
{
    public class QueueTTaskService : IQueueTTaskService
    {
        public const string JsonContentType = "application/json";

        public const string MessageType = "task";

        private readonly ILogger<QueueTTaskService> _logger;

        private readonly IServiceProvider _serviceProvider;

        private readonly IQueueTBroker _broker;

        private readonly QueueTTaskOptions _options;

        private IDictionary<string, TaskDefinition> TaskDefinitionsByName { get; }
            = new Dictionary<string, TaskDefinition>();

        private IDictionary<MethodInfo, TaskDefinition> TaskDefinitionsByMethod { get; }
            = new Dictionary<MethodInfo, TaskDefinition>();

        public QueueTTaskService(
            ILogger<QueueTTaskService> logger,
            IServiceProvider serviceProvider,
            IQueueTBroker broker,
            IOptions<QueueTTaskOptions> taskOptions)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _broker = broker;

            _options = taskOptions.Value;

            foreach (var taskDefinition in _options.Tasks)
                AddTask(taskDefinition);
        }

        public void AddTask(TaskDefinition taskDefinition)
        {
            if (taskDefinition == null)
                throw new ArgumentNullException(nameof(taskDefinition));

            if (TaskDefinitionsByName.ContainsKey(taskDefinition.Name))
                throw new ArgumentException($"Attempting to add task with duplcate name [{taskDefinition.Name}]");

            _logger.LogInformation($"Registering task: {taskDefinition.Name}");

            TaskDefinitionsByName.Add(taskDefinition.Name, taskDefinition);
            TaskDefinitionsByMethod.Add(taskDefinition.Method, taskDefinition);
        }

        public async Task<TaskMessage> DispatchAsync(MethodInfo methodInfo, IDictionary<string, object> arguments)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            if (!TaskDefinitionsByMethod.TryGetValue(methodInfo, out TaskDefinition definition))
                throw new ArgumentException($"Method [{methodInfo.Name}] must be registered before dispatching."); // TODO: Make custom exception

            var message = new TaskMessage
            {
                Name = definition.Name,
                Arguments = arguments
            };

            byte[] serializedMessage = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

            var queueTMessage = new QueueTMessage {
                Id = Guid.NewGuid().ToString(),
                ContentType = JsonContentType,
                Properties = new Dictionary<string, string>(),
                MessageType = MessageType,
                EncodedBody = serializedMessage,
                Created = DateTime.UtcNow
            };

            var targetQueue = definition.QueueName ?? _options.DefaultQueueName;

            await _broker.SendAsync(targetQueue, queueTMessage);

            return message;
        }

        public object[] GetParametersForTask(TaskDefinition taskDefinition, IDictionary<string, object> taskArguments)
        {
            var argumentList = new List<object>(taskDefinition.Parameters.Length);
            var missingArguments = new List<string>();

            foreach (var paramInfo in taskDefinition.Parameters)
            {
                if (taskArguments.TryGetValue(paramInfo.Name, out var value))
                    argumentList.Add(value);
                else if (paramInfo.IsOptional)
                    argumentList.Add(paramInfo.DefaultValue);
                else
                    missingArguments.Add(paramInfo.Name);
            }

            if (0 < missingArguments.Count)
                throw new ArgumentException($"Message for task [{taskDefinition.Name}] missing arguments: {string.Join(", ", missingArguments)}");

            return argumentList.ToArray();
        }

        public async Task<object> ExecuteTaskMessageAsync(TaskMessage message)
        {
            if (null == message)
                throw new ArgumentNullException(nameof(message));

            if (!TaskDefinitionsByName.TryGetValue(message.Name, out var taskDefinition))
                throw new ArgumentException($"Task Naame [{message.Name}] is not registered."); // TODO: Make custom exception

            var methodClass = _serviceProvider.GetService(taskDefinition.Method.DeclaringType);

            var retVal = taskDefinition.Method.Invoke(methodClass, GetParametersForTask(taskDefinition, message.Arguments));
            if (retVal is Task)
            {
                var retTask = retVal as Task;
                var resultProperty = retTask.GetType().GetProperty("Result");
                await retTask;
                retVal = resultProperty != null ?
                    resultProperty.GetValue(retVal) :
                    null;
            }
            return retVal;
        }
    }
}
