using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using QueueT;
using QueueT.Brokers;
using System.Linq;

namespace AspNetCoreWebApp.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class QueuesController : ControllerBase
    {
        public QueueTServiceOptions _options;

        private InMemoryBroker Broker => (InMemoryBroker)_options.Broker;

        private string[] GetQueueNames() => Broker.Queues.Select(x => x.Key).ToArray();

        public QueuesController(IOptions<QueueTServiceOptions> queueTOptions)
        {
            _options = queueTOptions.Value;
        }

        public IActionResult GetQueues()
        {
            return new OkObjectResult(new {
                items = GetQueueNames().Select(x=>GetQueueObject(x))
            });
        }

        private object GetQueueObject(string queueName)
        {
            return new
            {
                name = queueName,
                monitored = _options.Queues.Contains(queueName),
                length = Broker.Queues[queueName].Count
            };
        }

        [HttpGet("{queueName}")]
        public IActionResult GetQueue(string queueName)
        {
            var queues = GetQueueNames();
            if (queues.Contains(queueName))
                return NotFound();

            return new OkObjectResult(GetQueueObject(queueName));
        }

        [HttpPut("{queueName}")]
        public IActionResult UpdateQueue(string queueName, [FromBody] JObject jObject)
        {
            foreach(var prop in jObject.Properties())
            {
                switch (prop.Name)
                {
                    case "monitored":
                        var shouldMonitor = prop.Value.ToObject<bool>();
                        if (shouldMonitor)
                            _options.Queues.Add(queueName);
                        else
                            _options.Queues.Remove(queueName);
                        break;
                    default:
                        return new BadRequestResult();
                }
            }

            return new OkObjectResult(GetQueueObject(queueName));
        }
    }
}