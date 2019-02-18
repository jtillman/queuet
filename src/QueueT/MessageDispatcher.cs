using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace QueueT
{

    public class MessageDispatcher : IMessageDispatcher
    {
        public const string JsonContentType = "application/json";

        private readonly QueueTServiceOptions _options;

        public MessageDispatcher(
            IOptions<QueueTServiceOptions> options)
        {
            _options = options.Value;
        }

        public async Task<QueueTMessage> SendMessageAsync(string messageType, object message, DispatchOptions dispatchOptions = null)
        {
            dispatchOptions = dispatchOptions ?? new DispatchOptions();

            byte[] serializedBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

            var queuedMessage = new QueueTMessage
            {
                Id = Guid.NewGuid().ToString(),
                ContentType = JsonContentType,
                Properties = new Dictionary<string, string>(dispatchOptions.Properties),
                MessageType = messageType,
                EncodedBody = serializedBody,
                Created = DateTime.UtcNow
            };
            await _options.Broker.SendAsync(dispatchOptions.Queue ?? _options.DefaultQueueName, queuedMessage);
            return queuedMessage;
        }
    }
}
