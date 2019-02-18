using System.Collections.Generic;

namespace QueueT
{
    public class DispatchOptions
    {
        public string Queue { get; set; }

        public IDictionary<string, string> Properties { get; set; }
            = new Dictionary<string, string>();
    }
}
