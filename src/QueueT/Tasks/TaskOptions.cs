using System.Collections.Generic;

namespace QueueT.Tasks
{
    public class TaskOptions
    {
        public string DefaultQueueName { get; set; }

        public List<TaskDefinition> Tasks { get; set; }

        public TaskOptions()
        {
            Tasks = new List<TaskDefinition>();
        }
    }
}