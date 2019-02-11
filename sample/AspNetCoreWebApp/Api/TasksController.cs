using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QueueT.Tasks;
using System.Linq;
using System.Text;

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

        public class DelayTaskRequest
        {
            public string Queue { get; set; }
            public string Arguments { get; set; }
        }

        [HttpPost("{taskName}/delay")]
        public IActionResult DelayTask(string taskName, [FromBody]DelayTaskRequest request)
        {
            var definition = _taskOptions.Tasks.FirstOrDefault(x => x.Name == taskName);
            if (null == definition)
                return NotFound();

            _taskService.DispatchAsync(definition,
                Encoding.UTF8.GetBytes(request.Arguments),
                new DispatchOptions { Queue = request.Queue });

            return new JsonResult(new { }) { StatusCode = 201 };
        }
    }
}