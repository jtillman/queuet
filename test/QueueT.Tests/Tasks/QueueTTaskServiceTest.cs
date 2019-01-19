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

namespace QueueT.Tests.Tasks
{
    public class TestTaskClass
    {

        public TestTaskClass() { }

        public int Multiply(int left, int right)
        {
            return left * right;
        }

        public async Task<int> MultiplyAsync(int left, int right)
        {
            await Task.Delay(50); // Add a 50 millisecond delay
            return left * right;
        }
    }

    public class QueueTTaskServiceTest
    {
        Mock<ILogger<QueueTTaskService>> _mockLogger;
        Mock<IServiceProvider> _mockServiceProvider;
        Mock<IQueueTBroker> _mockBroker;

        QueueTTaskService _taskService;
        MethodInfo _syncTestMethod;
        MethodInfo _asyncTestMethod;

        public QueueTTaskServiceTest()
        {
            _mockLogger = new Mock<ILogger<QueueTTaskService>>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockBroker = new Mock<IQueueTBroker>();
            _taskService = new QueueTTaskService(_mockLogger.Object, _mockServiceProvider.Object, _mockBroker.Object);
            _syncTestMethod = typeof(TestTaskClass).GetMethod(nameof(TestTaskClass.Multiply));
            _asyncTestMethod = typeof(TestTaskClass).GetMethod(nameof(TestTaskClass.MultiplyAsync));
        }

        [Fact]
        public void RegisterTask_Throws_Null_TaskMethod()
        {
            Assert.Throws<ArgumentNullException>(() => _taskService.RegisterTask(null, "taskName", "queueName"));
        }

        [Fact]
        public void RegisterTask_Throws_On_Duplicate_TaskName()
        {
            _taskService.RegisterTask(_syncTestMethod);
            Assert.Throws<ArgumentException>(() => _taskService.RegisterTask(_syncTestMethod));
        }

        [Fact]
        public void RegisterTask_Correctly_Sets_Method()
        {
            var definition = _taskService.RegisterTask(_syncTestMethod);
            Assert.Equal(_syncTestMethod, definition.Method);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void RegisterTask_Correctly_Defaults_TaskName(string taskName)
        {
            var definition = _taskService.RegisterTask(_syncTestMethod, taskName);
            Assert.Equal(_syncTestMethod.GetDefaultTaskNameForMethod(), definition.TaskName);
        }

        [Theory]
        [InlineData("mytask", "mytask")]
        [InlineData(" mytask", "mytask")]
        [InlineData("mytask ", "mytask")]
        public void RegisterTask_Correctly_Uses_TaskName(string givenTaskName, string expectedTaskName)
        {
            var definition = _taskService.RegisterTask(_syncTestMethod, givenTaskName);
            Assert.Equal(expectedTaskName, definition.TaskName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("    ")]
        public void RegisterTask_Correctly_Defaults_QueueName(string queueName)
        {
            var definition = _taskService.RegisterTask(_syncTestMethod, queueName: queueName);
            Assert.Null(definition.QueueName);
        }

        [Theory]
        [InlineData("myqueue", "myqueue")]
        public void RegisterTask_Correctly_Uses_QueueName(string givenQueueName, string expectedQueueName)
        {
            var definition = _taskService.RegisterTask(_syncTestMethod, queueName: givenQueueName);
            Assert.Equal(expectedQueueName, definition.QueueName);
        }

        [Fact]
        public void DispatchAsync_Throws_On_Null_Method()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _taskService.DispatchAsync(null, null));
        }

        [Fact]
        public void DispatchAsync_Throws_When_Not_Registered()
        {
            Assert.ThrowsAsync<ArgumentException>(async () => await _taskService.DispatchAsync(_syncTestMethod, null));
        }

        [Fact]
        public void DispatchAsync_Correctly_Uses_Dispatcher()
        {
            var taskName = "MyTask";
            var queueName = "MyQueue";

            var definition = _taskService.RegisterTask(_syncTestMethod, taskName, queueName);
            var arguments = new Dictionary<string, object> { { "left", 5 }, { "right", 8 } };

            var serializedArguments = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
                new
                {
                    name = "MyTask",
                    arguments = new
                    {
                        left = 5,
                        right = 8
                    }
                }));

            _mockBroker.Setup(
                d => d.SendAsync(
                    queueName,
                    
                    It.Is<QueueTMessage>(m =>
                        m.ContentType == QueueTTaskService.JsonContentType &&
                        m.MessageType == QueueTTaskService.MessageType && 
                        m.EncodedBody.SequenceEqual(serializedArguments))))
                    .Returns(Task.CompletedTask)
                    .Verifiable("Message is not being correctly dispatched");

            var message = _taskService.DispatchAsync(_syncTestMethod, arguments).Result;

            _mockBroker.Verify();

            Assert.Equal(definition.TaskName, message.Name);
            Assert.Equal(arguments, message.Arguments);
        }

        [Fact]
        public void ExecuteTaskMessageAsync_Throws_On_Unknown_TaskName()
        {
            var message = new TaskMessage
            {
                Name = "UnRegistered",
                Arguments = new Dictionary<string, object>()
            };

            Assert.ThrowsAsync<ArgumentException>(async () => await _taskService.ExecuteTaskMessageAsync(message));
        }

        [Theory]
        [InlineData(1, 1, 1)]
        [InlineData(1, 0, 0)]
        [InlineData(9, 9, 81)]
        public void ExecuteTaskMessageAsync_Returns_Value(int left, int right, int expectedResult)
        {
            var taskName = "MultiplyTask";
            var message = new TaskMessage
            {
                Name = taskName,
                Arguments = new Dictionary<string, object> { { nameof(left), left }, { nameof(right), right } }
            };
            _mockServiceProvider.Setup(sp => sp.GetService(_syncTestMethod.DeclaringType))
                .Returns(new TestTaskClass());

            _taskService.RegisterTask(_syncTestMethod, taskName);
            var result = _taskService.ExecuteTaskMessageAsync(message).Result;
            Assert.Equal(result, expectedResult);
        }

        [Theory]
        [InlineData(1, 1, 1)]
        [InlineData(2, 2, 4)]
        [InlineData(3, 3, 9)]
        [InlineData(4, 4, 16)]
        [InlineData(5, 5, 25)]
        public void ExecuteTaskMessageAsync_Awaits_Tasks_And_Returns_Value(int left, int right, int expectedResult)
        {
            var taskName = "WaitMultiplyTask";
            var message = new TaskMessage
            {
                Name = taskName,
                Arguments = new Dictionary<string, object>
                {
                    { "left", left },
                    { "right", right }
                }
            };

            _mockServiceProvider.Setup(sp => sp.GetService(_asyncTestMethod.DeclaringType))
                .Returns(new TestTaskClass());

            _taskService.RegisterTask(_asyncTestMethod, taskName);
            var result = _taskService.ExecuteTaskMessageAsync(message).Result;
            Assert.Equal(result, expectedResult);
        }

        [Fact]
        public void GetParametersForTask_Throws_When_Parameter_Not_Present()
        {
            var definition = new TaskDefinition("testTask", _syncTestMethod, "testQueue");
            var arguments = new Dictionary<string, object> { };
            Assert.Throws<ArgumentException>(() => _taskService.GetParametersForTask(definition, arguments));
        }

        [Fact]
        public void GetParametersForTask_Creates_Correct_Array()
        {
            var definition = new TaskDefinition("testTask", _syncTestMethod, "testQueue");
            var arguments = new Dictionary<string, object>
            {
                {"left", 1 },
                {"right", 2 }
            };

            var parameters = _taskService.GetParametersForTask(definition, arguments);
            Assert.Equal(new object[] { 1, 2 }, parameters);
        }

        [Fact]
        public void GetParametersForTask_Ignores_Extra_Arguments()
        {
            var definition = new TaskDefinition("testTask", _syncTestMethod, "testQueue");

            var arguments = new Dictionary<string, object>
            {
                {"left", 3 },
                {"middle", "number" },
                {"right", 4 }
            };

            var parameters = _taskService.GetParametersForTask(definition, arguments);
            Assert.Equal(new object[] { 3, 4 }, parameters);
        }
    }
}
