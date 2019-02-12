using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QueueT.Worker
{
    public class QueueTWorkerService : BackgroundService
    {
        ILogger<QueueTWorkerService> _logger;
        QueueTServiceOptions _options;
        IList<IMessageHandler> _messageHandlers = new List<IMessageHandler>();
        IServiceProvider _serviceProvider;

        public QueueTWorkerService(
            ILogger<QueueTWorkerService> logger,
            IOptions<QueueTServiceOptions> options,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _options = options.Value;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach(var handlerType in _options.MessageHandlerTypes)
            {
                _logger.LogInformation($"Retrieve instance of handler: {handlerType.Name}");
                _messageHandlers.Add(_serviceProvider.GetRequiredService(handlerType) as IMessageHandler);
            }

            _logger.LogInformation("Starting to receive from queues");

            var queueIndex = 0;
            var taskList = new List<Task>();
            var activeQueues = new string[] { };

            while (!stoppingToken.IsCancellationRequested)
            {
                // Detect Queue List Changes
                var currentQueues = _options.Queues.ToArray();
                if (!currentQueues.SequenceEqual(activeQueues))
                {
                    activeQueues = currentQueues;
                    queueIndex = 0;
                }

                // Trim completed tasks
                taskList = taskList.Where(t => !t.IsCompleted).ToList();

                // Wait idle time when no queues listed
                if ( 0 == activeQueues.Length)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5)); // FIXME: Magic Number for wait time
                    continue;
                }

                while(taskList.Count < _options.WorkerTaskCount)
                {
                    var queue = currentQueues[queueIndex];
                    taskList.Add(Task.Run(async () =>
                    {
                        await _options.Broker.ReceiveMessagesAsync(queue, _options.WorkerBatchSize, ProcessMessageAsync, stoppingToken);
                    }));

                    queueIndex = activeQueues.Length <= queueIndex + 1 ? 0 : queueIndex + 1;
                }
                await Task.WhenAny(taskList);
            }
            await Task.WhenAll(taskList);
        }

        public async Task ProcessMessageAsync(QueueTMessage message, CancellationToken cancellationToken)
        {
            foreach(var messageHandler in _messageHandlers)
            {
                if (!messageHandler.CanHandleMessage(message))
                    continue;
                await messageHandler.HandleMessage(message);
                return;
            }

            // There are no handlers for this message
            throw new MessageProcessingException(MessageAction.Acknowledge, $"No plugins registered that may handle this message");
        }
    }
}
