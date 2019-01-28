using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QueueT.Worker
{
    public class QueueTWorkerService : BackgroundService
    {
        ILogger<QueueTWorkerService> _logger;
        QueueTServiceOptions _options;
        IList<IQueueTMessageHandler> _messageHandlers = new List<IQueueTMessageHandler>();
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
            int queueIndex = 0;

            foreach(var handlerType in _options.MessageHandlerTypes)
            {
                _logger.LogInformation($"Retrieve instance of handler: {handlerType.Name}");
                _messageHandlers.Add(_serviceProvider.GetRequiredService(handlerType) as IQueueTMessageHandler);
            }

            _logger.LogInformation("Starting to receive from queues");

            while (!stoppingToken.IsCancellationRequested)
            {
                var queue = _options.Queues[queueIndex];
                //_logger.LogDebug($"Attempting to receive {_options.WorkerBatchSize} message on queue: {queue}");
                await _options.Broker.ReceiveMessagesAsync(queue, _options.WorkerBatchSize, ProcessMessageAsync, stoppingToken);

                queueIndex = _options.Queues.Count <= queueIndex + 1 ? 0 : queueIndex + 1;
            }
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
