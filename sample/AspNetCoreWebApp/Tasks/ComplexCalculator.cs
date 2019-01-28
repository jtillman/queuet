using Microsoft.Extensions.Logging;
using QueueT.Tasks;

namespace AspNetCoreWebApp.Tasks
{
    public class ComplexCalculator
    {
        ILogger<ComplexCalculator> _logger;

        public ComplexCalculator(ILogger<ComplexCalculator> logger)
        {
            _logger = logger;
        }

        [QueuedTask]
        public int Multiply(int right, int left) => right * left;

        [QueuedTask]
        public int Add(int right, int left) => right + left;

        [QueuedTask(TaskName = "SubstractTask")]
        public int Substract(int right, int left) => right - left;
    }
}
