using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QueueT.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueueT.Notifications
{
    public class NotificationMessageHandler : IMessageHandler
    {
        private readonly ILogger<NotificationMessageHandler> _logger;

        private readonly ITaskService _taskService;

        private readonly INotificationRegistry _notificationRegistry;

        private readonly NotificationOptions _options;

        public NotificationMessageHandler(
            ILogger<NotificationMessageHandler> logger,
            IOptions<NotificationOptions> options,
            INotificationRegistry notificationRegistry,
            ITaskService taskService)
        {
            _logger = logger;
            _options = options.Value;
            _notificationRegistry = notificationRegistry;
            _taskService = taskService;
        }

        public bool CanHandleMessage(QueueTMessage message) => message.MessageType == NotificationMessage.MessageType;

        public async Task HandleMessage(QueueTMessage message)
        {
            if (!message.Properties.TryGetValue(NotificationMessage.TopicPropertyKey, out var topic))
                throw new MessageProcessingException(message: $"Topic name not present");

            var notificationDefinition = _notificationRegistry.GetNotificationByTopic(topic);
            var subscriptions = _notificationRegistry.GetSubscriptions(notificationDefinition);
            if (0 == subscriptions.Length)
                return;

            JObject argument = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.EncodedBody)) as JObject;
            var properties = argument.Properties().ToArray();

            foreach (var subscription in subscriptions)
            {
                var taskArguements = new Dictionary<string, object>();
                var options = new DispatchOptions{ Queue = subscription.TaskQueue ?? _options.DefaultQueueName };

                foreach (var parameter in subscription.Parameters)
                {
                    var property = properties.FirstOrDefault(x => x.Name.Equals(parameter.Name, StringComparison.InvariantCultureIgnoreCase));
                    if (null != property)
                    {
                        taskArguements[parameter.Name] = property.Value;
                    }
                    else if (!parameter.IsOptional)
                    {
                        _logger.LogCritical($"No matching parameter for parameter: {parameter.Name}");
                    }
                }

                await _taskService.DelayAsync(subscription.Method, taskArguements, options);
            }
        }
    }
}
