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
    public class TaskService : ITaskService, IMessageHandler
    {
        public const string JsonContentType = "application/json";

        public const string MessageType = "task";

        public const string TaskNamePropertyKey = "taskName";

        private readonly ILogger<TaskService> _logger;

        private readonly IServiceProvider _serviceProvider;

        private readonly QueueTServiceOptions _appOptions;

        private readonly TaskServiceOptions _taskOptions;

        private readonly ITaskRegistry _taskRegistry;

        public TaskService(
            ILogger<TaskService> logger,
            IServiceProvider serviceProvider,
            IOptions<QueueTServiceOptions> appOptions,
            IOptions<TaskServiceOptions> taskOptions,
            ITaskRegistry taskRegistry)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _appOptions = appOptions.Value;
            _taskOptions = taskOptions.Value;
            _taskRegistry = taskRegistry;
        }

        public async Task<TaskMessage> DelayAsync<T>(Expression<Action<T>> expression, TaskDispatchOptions options = null) => await _DelayAsync(expression?.Body as MethodCallExpression, options);

        public async Task<TaskMessage> DelayAsync<T>(Expression<Func<T, Task>> expression, TaskDispatchOptions options = null) => await _DelayAsync(expression?.Body as MethodCallExpression, options);

        private async Task<TaskMessage> _DelayAsync(MethodCallExpression methodExpression, TaskDispatchOptions options = null)
        {
            if (null == methodExpression)
                throw new ArgumentException("Expression must be a method call");

            var definition = _taskRegistry.GetTaskByMethod(methodExpression.Method);

            var arguments = definition.CreateArgumentsFromCall(methodExpression);
            return await DispatchAsync(definition, arguments, options);
        }

        public async Task<TaskMessage> DelayAsync(MethodInfo methodInfo, IDictionary<string, object> arguments, TaskDispatchOptions options = null)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            var definition = _taskRegistry.GetTaskByMethod(methodInfo);
            return await DispatchAsync(definition, arguments, options);
        }

        public async Task<string> DispatchAsync(TaskDefinition definition, byte[] encodedArguments, TaskDispatchOptions options = null)
        {
            var messageId = Guid.NewGuid().ToString();

            var queueTMessage = new QueueTMessage
            {
                Id = messageId,
                ContentType = JsonContentType,
                Properties = new Dictionary<string, string> { { TaskNamePropertyKey, definition.Name } },
                MessageType = MessageType,
                EncodedBody = encodedArguments,
                Created = DateTime.UtcNow
            };

            var targetQueue = options?.Queue ?? definition.QueueName ?? _taskOptions.DefaultQueueName ?? _appOptions.DefaultQueueName;

            await _appOptions.Broker.SendAsync(targetQueue, queueTMessage);
            return messageId;
        }

        public async Task<TaskMessage> DispatchAsync(TaskDefinition definition, IDictionary<string, object> arguments, TaskDispatchOptions options = null)
        {
            var message = new TaskMessage
            {
                Name = definition.Name,
                Arguments = arguments
            };

            byte[] serializedArguments = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(arguments));

            await DispatchAsync(definition, serializedArguments, options);

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

            var taskDefinition = _taskRegistry.GetTaskByName(message.Name);

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

            var definition = _taskRegistry.GetTaskByName(taskName);

            var sw = new Stopwatch();
            sw.Start();

            var arguments = new Dictionary<string, object>();

            try
            {
                var jsonArguments = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.EncodedBody)) as JObject;
                foreach (var property in jsonArguments.Properties())
                {
                    var parameter = definition.Parameters.FirstOrDefault(p => p.Name.Equals(property.Name));
                    if (null == parameter)
                        continue;

                    arguments[parameter.Name] = property.Value.ToObject(parameter.ParameterType);
                }
            }catch(Exception ex)
            {
                _logger.LogCritical("Unable to deserialize task: TaskName={TaskName} Exception={exception}", taskName, ex.Message);
                return;
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
