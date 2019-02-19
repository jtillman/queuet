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
        public const string MessageType = "task";

        public const string TaskNamePropertyKey = "taskName";

        private readonly ILogger<TaskService> _logger;

        private readonly IServiceProvider _serviceProvider;

        private readonly QueueTServiceOptions _appOptions;

        private readonly TaskServiceOptions _taskOptions;

        private readonly ITaskRegistry _taskRegistry;

        private readonly IMessageDispatcher _messageDispatcher;


        public TaskService(
            ILogger<TaskService> logger,
            IServiceProvider serviceProvider,
            IOptions<QueueTServiceOptions> appOptions,
            IOptions<TaskServiceOptions> taskOptions,
            ITaskRegistry taskRegistry,
            IMessageDispatcher messageDispatcher)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _appOptions = appOptions.Value;
            _taskOptions = taskOptions.Value;
            _taskRegistry = taskRegistry;
            _messageDispatcher = messageDispatcher;
        }

        private TaskDefinition GetTaskDefinition(MethodInfo methodInfo)
        {
            return _taskRegistry.GetTaskByMethod(methodInfo) ?? throw new ArgumentException($"Method [{methodInfo.Name}] must be registered before dispatching.");
        }

        private TaskDefinition GetTaskDefinition(string taskName)
        {
            return _taskRegistry.GetTaskByName(taskName) ?? throw new ArgumentException($"Task Naame [{taskName}] is not registered.");
        }

        public async Task<TaskMessage> DelayAsync<T>(Expression<Action<T>> expression, DispatchOptions options = null) => await _DelayAsync(expression?.Body as MethodCallExpression, options);

        public async Task<TaskMessage> DelayAsync<T>(Expression<Func<T, Task>> expression, DispatchOptions options = null) => await _DelayAsync(expression?.Body as MethodCallExpression, options);

        private async Task<TaskMessage> _DelayAsync(MethodCallExpression methodExpression, DispatchOptions options = null)
        {
            if (null == methodExpression)
                throw new ArgumentException("Expression must be a method call");

            var definition = GetTaskDefinition(methodExpression.Method);

            var arguments = definition.CreateArgumentsFromCall(methodExpression);
            return await DispatchAsync(definition, arguments, options);
        }

        public async Task<TaskMessage> DelayAsync(MethodInfo methodInfo, object arguments, DispatchOptions options = null)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            var definition = GetTaskDefinition(methodInfo);
            return await DispatchAsync(definition, arguments, options);
        }

        public async Task<TaskMessage> DelayAsync(string taskName, object arguments, DispatchOptions options = null)
        {
            var definition = GetTaskDefinition(taskName);
            return await DispatchAsync(definition, arguments, options);
        }

        private async Task<TaskMessage> DispatchAsync(TaskDefinition definition, object arguments, DispatchOptions options = null)
        {
            options = options ?? new DispatchOptions();
            options.Queue = options.Queue ?? definition.QueueName ?? _taskOptions.DefaultQueueName;

            options.Properties[TaskNamePropertyKey] = definition.Name;

            var message = await _messageDispatcher.SendMessageAsync(MessageType, arguments, options);

            return new TaskMessage
            {
                Name = definition.Name
            };
        }

        public async Task<object> ExecuteTaskMessageAsync(TaskMessage message)
        {
            if (null == message)
                throw new ArgumentNullException(nameof(message));

            var taskDefinition = GetTaskDefinition(message.Name);

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

            var definition = GetTaskDefinition(taskName);

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
