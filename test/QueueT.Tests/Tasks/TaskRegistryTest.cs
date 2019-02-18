using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using QueueT.Tasks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace QueueT.Tests.Tasks
{
    public class TaskRegistryTest
    {
        TaskServiceOptions _serviceOptions;

        TaskRegistry _taskRegistry;

        MethodInfo _testMethod;

        public TaskRegistryTest()
        {
            _serviceOptions = new TaskServiceOptions();
            _taskRegistry = new TaskRegistry(NullLogger<TaskRegistry>.Instance, Options.Create(_serviceOptions));
            _testMethod = typeof(TestTaskClass).GetMethod(nameof(TestTaskClass.Multiply));
        }

        [Fact]
        public void AddTask_Throws_On_Null_TaskDefinition()
        {
            Assert.Throws<ArgumentNullException>(() => _taskRegistry.AddTask(null));
        }

        [Fact]
        public void AddTask_Throws_On_Duplicate_TaskName()
        {
            var defintion = new TaskDefinition("task", _testMethod, "queue");
            _taskRegistry.AddTask(defintion);
            Assert.Throws<ArgumentException>(() => _taskRegistry.AddTask(defintion));
        }
    }
}
