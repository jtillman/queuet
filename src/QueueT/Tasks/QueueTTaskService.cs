using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QueueT.Tasks
{
    public class QueueTTaskService : IQueueTTaskService, IQueueTMessageHandler
    {
        public const string JsonContentType = "application/json";

        public const string MessageType = "task";

        public const string TaskNamePropertyKey = "taskName";

        private readonly ILogger<QueueTTaskService> _logger;

        private readonly IServiceProvider _serviceProvider;

        private readonly QueueTServiceOptions _appOptions;
        private readonly QueueTTaskOptions _taskOptions;

        private IDictionary<string, TaskDefinition> TaskDefinitionsByName { get; }
            = new Dictionary<string, TaskDefinition>();

        private IDictionary<MethodInfo, TaskDefinition> TaskDefinitionsByMethod { get; }
            = new Dictionary<MethodInfo, TaskDefinition>();

        public QueueTTaskService(
            ILogger<QueueTTaskService> logger,
            IServiceProvider serviceProvider,
            IOptions<QueueTServiceOptions> appOptions,
            IOptions<QueueTTaskOptions> taskOptions)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _appOptions = appOptions.Value;
            _taskOptions = taskOptions.Value;

            foreach (var taskDefinition in _taskOptions.Tasks)
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

        public async Task<TaskMessage> DelayAsync<T>(Expression<Action<T>> expression) => await _DelayAsync(expression?.Body as MethodCallExpression);

        public async Task<TaskMessage> DelayAsync<T>(Expression<Func<T, Task>> expression) => await _DelayAsync(expression?.Body as MethodCallExpression);

        private async Task<TaskMessage> _DelayAsync(MethodCallExpression methodExpression)
        {
            if (null == methodExpression)
                throw new ArgumentException("Expression must be a method call");

            if (!TaskDefinitionsByMethod.TryGetValue(methodExpression.Method, out var taskDefinition))
            {
                throw new ArgumentException("Method is not registered task");
            }

            var definition = GetForMethod(methodExpression.Method);
            var arguments = definition.CreateArgumentsFromCall(methodExpression);
            return await DispatchAsync(definition, arguments);
        }

        internal TaskDefinition GetForMethod(MethodInfo methodInfo)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            if (!TaskDefinitionsByMethod.TryGetValue(methodInfo, out TaskDefinition definition))
                throw new ArgumentException($"Method [{methodInfo.Name}] must be registered before dispatching."); // TODO: Make custom exception

            return definition;
        }

        public async Task<TaskMessage> DelayAsync(MethodInfo methodInfo, IDictionary<string, object> arguments)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            if (!TaskDefinitionsByMethod.TryGetValue(methodInfo, out TaskDefinition definition))
                throw new ArgumentException($"Method [{methodInfo.Name}] must be registered before dispatching."); // TODO: Make custom exception

            return await DispatchAsync(definition, arguments);
        }

        internal async Task<TaskMessage> DispatchAsync(TaskDefinition definition, IDictionary<string, object> arguments)
        {
            var message = new TaskMessage
            {
                Name = definition.Name,
                Arguments = arguments
            };

            byte[] serializedArguments = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(arguments));

            var queueTMessage = new QueueTMessage
            {
                Id = Guid.NewGuid().ToString(),
                ContentType = JsonContentType,
                Properties = new Dictionary<string, string> { { TaskNamePropertyKey, definition.Name } },
                MessageType = MessageType,
                EncodedBody = serializedArguments,
                Created = DateTime.UtcNow
            };

            var targetQueue = definition.QueueName ?? _taskOptions.DefaultQueueName ?? _appOptions.DefaultQueueName;

            await _appOptions.Broker.SendAsync(targetQueue, queueTMessage);

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

            var methodClass = ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, taskDefinition.Method.DeclaringType);

            var retVal = taskDefinition.Method.Invoke(methodClass, taskDefinition.GetParametersFromArguments(message.Arguments));
            if (retVal is Task)
            {
                var retTask = retVal as Task;
                var resultProperty = retTask.GetType().GetProperty("Result");
                await retTask;
                retVal = resultProperty?.GetValue(retVal);
            }
            return retVal;
        }

        public bool CanHandleMessage(QueueTMessage message) => string.Equals(message?.MessageType, MessageType, StringComparison.InvariantCultureIgnoreCase);

        public async Task HandleMessage(QueueTMessage message)
        {
            if (!message.Properties.TryGetValue(TaskNamePropertyKey, out var taskName))
                throw new ArgumentException("TaskName not present in message");

            if (!TaskDefinitionsByName.TryGetValue(taskName, out var definition))
                throw new ArgumentException($"Received task with unknown name {taskName}");

            var sw = new Stopwatch();
            sw.Start();

            var arguments = new Dictionary<string, object>();
            var jsonArguments = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.EncodedBody)) as JObject;
            foreach(var property in jsonArguments.Properties())
            {
                var parameter = definition.Parameters.FirstOrDefault(p => p.Name.Equals(property.Name));
                if (null == parameter)
                    continue;

                arguments[parameter.Name] = property.Value.ToObject(parameter.ParameterType);
            }

            var taskMessage = new TaskMessage { Name = taskName, Arguments = arguments };

            _logger.LogDebug("TaskMessage Deserialized: TaskName={TaskName} TotalMilliseconds={TotalMilliseconds} MessageSize={MessageSize}", taskMessage.Name, sw.Elapsed.TotalMilliseconds, message.EncodedBody.Length);

            try
            {
                _logger.LogInformation("Task Starting: TaskName={TaskName}", taskMessage.Name);
                sw.Restart();
                await ExecuteTaskMessageAsync(taskMessage);
                _logger.LogInformation("Task Completed: TaskName={TaskName} TotalMilliseconds={TotalMilliseconds}", taskMessage.Name, sw.Elapsed.TotalMilliseconds);
            }catch (Exception ex)
            {
                _logger.LogCritical("Task Failed: TaskName={TaskName} TotalMilliseconds={TotalMilliseconds} Exception={ex.Message}", taskMessage.Name, sw.Elapsed.TotalMilliseconds, ex.Message);
                // We can add some certaim properties to the message
                // This will allow us to implement certain features like
                // Task Retry
                // Dead Lettering
            }
        }
    }
}
