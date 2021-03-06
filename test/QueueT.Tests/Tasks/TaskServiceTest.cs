﻿using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using QueueT.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace QueueT.Tests.Tasks
{

    public class TaskServiceTest
    {
        Mock<IServiceProvider> _mockServiceProvider;
        Mock<IQueueTBroker> _mockBroker;
        Mock<IMessageDispatcher> _mockDispatcher;

        TaskRegistry _taskRegistry;
        TaskService _taskService;
        TaskServiceOptions _taskServiceOptions;
        QueueTServiceOptions _queueServiceOptions;
        MethodInfo _syncTestMethod;
        MethodInfo _asyncTestMethod;

        public TaskServiceTest()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockBroker = new Mock<IQueueTBroker>();
            _mockDispatcher = new Mock<IMessageDispatcher>();

            _taskServiceOptions = new TaskServiceOptions();
            _queueServiceOptions = new QueueTServiceOptions{ Broker = _mockBroker.Object };


            _taskRegistry = new TaskRegistry(
                NullLogger<TaskRegistry>.Instance,
                Options.Create(_taskServiceOptions));

            _taskService = new TaskService(
                NullLogger<TaskService>.Instance,
                _mockServiceProvider.Object,
                Options.Create(_queueServiceOptions),
                Options.Create(_taskServiceOptions),
                _taskRegistry,
                _mockDispatcher.Object);

            _syncTestMethod = typeof(TestTaskClass).GetMethod(nameof(TestTaskClass.Multiply));
            _asyncTestMethod = typeof(TestTaskClass).GetMethod(nameof(TestTaskClass.MultiplyAsync));
        }

        [Fact]
        public void DelayAsync_Throws_On_Null_Method()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _taskService.DelayAsync((Expression<Action<string>>)null, null));
        }

        [Fact]
        public void DelayAsync_Throws_When_Not_Registered()
        {
            Assert.ThrowsAsync<ArgumentException>(async () => await _taskService.DelayAsync(_syncTestMethod, null));
        }

        [Fact]
        public void DelayAsync_With_Expression_Dispatches_Correctly()
        {
            var taskName = "task";
            var queueName = "queue";
            var left = 5;
            var right = 6;

            var definition = new TaskDefinition(taskName, _syncTestMethod, queueName);

            _mockDispatcher.Setup(dispatcher => dispatcher.SendMessageAsync(
                TaskService.MessageType,
                It.Is<Dictionary<string, object>>(args => (int)args["left"] == left && (int)args["right"] == right),
                It.IsAny<DispatchOptions>()))
                .Returns(Task.FromResult(new QueueTMessage { }))
                .Verifiable();

            _taskRegistry.AddTask(definition);
            var message = _taskService.DelayAsync<TestTaskClass>(c => c.Multiply(5, 6)).Result;

            _mockDispatcher.Verify();
        }

        [Fact]
        public void DelayAsync_Correctly_Uses_Dispatcher()
        {
            var taskName = "MyTask";
            var queueName = "MyQueue";

            _taskRegistry.AddTask(new TaskDefinition(taskName, _syncTestMethod, queueName));
            var arguments = new Dictionary<string, object> { { "left", 5 }, { "right", 8 } };

            _mockDispatcher.Setup(dispatcher => dispatcher.SendMessageAsync(
                TaskService.MessageType,
                It.Is<Dictionary<string, object>>(args => (int)args["left"] == 5 && (int)args["right"] == 8),
                It.IsAny<DispatchOptions>()))
                .Returns(Task.FromResult(new QueueTMessage { }))
                .Verifiable("Message is not being correctly dispatched");

            var message = _taskService.DelayAsync(_syncTestMethod, arguments, new DispatchOptions()).Result;

            _mockBroker.Verify();

            Assert.Equal(taskName, message.Name);
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

            _taskRegistry.AddTask(new TaskDefinition(taskName, _syncTestMethod, "queue"));
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

            _taskRegistry.AddTask(new TaskDefinition(taskName, _asyncTestMethod, "queue"));
            var result = _taskService.ExecuteTaskMessageAsync(message).Result;
            Assert.Equal(result, expectedResult);
        }
    }
}