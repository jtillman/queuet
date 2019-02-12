using System.Collections.Generic;

namespace QueueT.Tasks
{
    public class TaskServiceOptions
    {
        public string DefaultQueueName { get; set; }

        public List<TaskDefinition> Tasks { get; set; }

        public TaskServiceOptions()
        {
            Tasks = new List<TaskDefinition>();
        }
    }
}