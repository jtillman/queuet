using System;

namespace QueueT.Tasks
{
    public class QueuedTaskAttribute : Attribute
    {
        public string Name { get; set; }

        public string Queue { get; set; }
    }
}
