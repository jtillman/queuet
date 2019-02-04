using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using QueueT.Tasks;

namespace AspNetCoreWebApp.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private QueueTTaskOptions _taskOptions;
        private IQueueTTaskService _taskService;

        public TasksController(IOptions<QueueTTaskOptions> taskOptions, IQueueTTaskService taskService)
        {
            _taskOptions = taskOptions.Value;
            _taskService = taskService;
        }

        private object GetTaskObject(TaskDefinition definition)
        {
            return new
            {
                name = definition.Name,
                method = definition.Method.GetDefaultTaskNameForMethod(),
                queue = definition.QueueName
            };
        }

        public IActionResult GetTasks()
        {
            return Ok(new
            {
                items = _taskOptions.Tasks.Select(x=>GetTaskObject(x)).ToList()
            });
        }

        [HttpPost("{taskName}/invoke")]
        public IActionResult InvokeTask(string taskName, JObject arguments)
        {
            var definition = _taskOptions.Tasks.FirstOrDefault(x => x.Name == taskName);
            if (null == definition)
                return NotFound();

            _taskService.DelayAsync(definition.Method, new Dictionary<string, object>() { });

            return new ViewResult { StatusCode = 201 };
        }
    }
}