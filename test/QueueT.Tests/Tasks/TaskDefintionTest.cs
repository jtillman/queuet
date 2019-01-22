using QueueT.Tasks;
using System;
using System.Reflection;
using Xunit;

namespace QueueT.Tests.Tasks
{
    public class TaskDefintionTest
    {
        public void TestMethod() { }

        public MethodInfo _testMethodInfo;

        public TaskDefintionTest()
        {
            _testMethodInfo = GetType().GetMethod(nameof(TestMethod));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TaskDefinition_Throws_On_Bad_TaskName(string taskName)
        {
            Assert.Throws<ArgumentException>(()=> new TaskDefinition(taskName, _testMethodInfo, "queue"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("    ")]
        public void TaskDefintion_Throws_On_Bad_QueueName(string queueName)
        {
            Assert.Throws<ArgumentException>(() => new TaskDefinition("task", _testMethodInfo, queueName));
        }

        [Theory]
        [InlineData("mytask", "mytask")]
        [InlineData(" mytask", "mytask")]
        [InlineData("mytask ", "mytask")]
        public void TaskDefinition_Correctly_Sets_TaskName(string actualTaskName, string expectedTaskName)
        {
            Assert.Equal(expectedTaskName, new TaskDefinition(actualTaskName, _testMethodInfo, "queue").Name);
        }

        [Theory]
        [InlineData("queue", "queue")]
        public void TaskDefinition_Correctly_Sets_QueueName(string actualQueueName, string expectedQueueName)
        {
            Assert.Equal(expectedQueueName, new TaskDefinition("task", _testMethodInfo, actualQueueName).QueueName);
        }

        [Fact]
        public void TaskDefintiion_Correctly_Sets_Method()
        {
            Assert.Equal(_testMethodInfo, new TaskDefinition("task", _testMethodInfo, "queue").Method);
        }


    }
}
