using QueueT.Tasks;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace QueueT.Tests.Tasks
{
    public class TaskDefintionTest
    {
#pragma warning disable xUnit1013 // Public method should be marked as test
        public void TestMethod(int intArgument, string stringArgument, string optionalStringArgument = "default")
        {
#pragma warning restore xUnit1013 // Public method should be marked as test
            Console.WriteLine($"TestMethod({intArgument}, {stringArgument}, {optionalStringArgument}");
        }

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

        [Fact]
        public void GetParametersFromArguments_Throws_When_Parameter_Not_Present()
        {
            var definition = new TaskDefinition("testTask", _testMethodInfo, "testQueue");
            var arguments = new Dictionary<string, object> { };
            Assert.Throws<ArgumentException>(() => definition.GetParametersFromArguments(arguments));
        }

        [Fact]
        public void GetParametersFromArguments_Creates_Correct_Array()
        {
            var definition = new TaskDefinition("testTask", _testMethodInfo, "testQueue");
            var arguments = new Dictionary<string, object>
            {
                {"intArgument", 1 },
                {"stringArgument", "string" },
                {"optionalStringArgument", "optional" }
            };

            var parameters = definition.GetParametersFromArguments(arguments);
            Assert.Equal(new object[] { 1, "string", "optional" }, parameters);
        }

        [Fact]
        public void GetParametersFromArguments_Ignores_Extra_Arguments()
        {
            var definition = new TaskDefinition("testTask", _testMethodInfo, "testQueue");

            var arguments = new Dictionary<string, object>
            {
                {"intArgument", 1 },
                {"stringArgument", "string" },
                {"optionalStringArgument", "optional"},
                {"extraArgument", "extra" }
            };

            var parameters = definition.GetParametersFromArguments(arguments);
            Assert.Equal(new object[] { 1, "string", "optional" }, parameters);
        }

        [Fact]
        public void GetParametersFromArguments_Defaults_Missing_Optional_Arguments()
        {
            var definition = new TaskDefinition("testTask", _testMethodInfo, "testQueue");

            var arguments = new Dictionary<string, object>
            {
                {"intArgument", 1 },
                {"stringArgument", "string" },
                {"extraArgument", "extra" }
            };

            var parameters = definition.GetParametersFromArguments(arguments);
            Assert.Equal(new object[] { 1, "string", "default" }, parameters);
        }
    }
}