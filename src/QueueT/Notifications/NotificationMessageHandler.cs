using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using QueueT.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace QueueT.Notifications
{
    public class NotificationMessageHandler : IMessageHandler
    {
        private readonly ILogger<NotificationMessageHandler> _logger;

        private readonly ITaskService _taskService;

        private readonly NotificationRegistry _notificationRegistry;

        private readonly NotificationOptions _options;

        public NotificationMessageHandler(
            ILogger<NotificationMessageHandler> logger,
            IOptions<NotificationOptions> options,
            NotificationRegistry notificationRegistry,
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

            var argument = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.EncodedBody));
            foreach (var subscription in subscriptions)
            {
                var taskArguements = new Dictionary<string, object>();
                var options = new DispatchOptions{ Queue = subscription.TaskQueue ?? _options.DefaultQueueName };
                
                await _taskService.DelayAsync(subscription.Method, argument, options);
            }
        }
    }
}
