using System.Collections.Generic;

namespace QueueT.Tasks
{
    public class QueueTTaskOptions
    {
        public string DefaultQueueName { get; set; }

        public List<TaskDefinition> Tasks { get; set; }

        public QueueTTaskOptions()
        {
            Tasks = new List<TaskDefinition>();
        }
    }
}