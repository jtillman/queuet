using System;

namespace QueueT.Tasks
{
    public class QueuedTaskAttribute : Attribute
    {
        public string TaskName { get; set; }

        public string QueueName { get; set; }
    }
}
