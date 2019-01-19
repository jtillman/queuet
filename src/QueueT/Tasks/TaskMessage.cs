using Newtonsoft.Json;
using System.Collections.Generic;

namespace QueueT.Tasks
{
    public class TaskMessage
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("arguments")]
        public IDictionary<string, object> Arguments { get; set; }
    }
}
