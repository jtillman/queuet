using Microsoft.Extensions.Logging;
using QueueT.Tasks;
using System;
using System.Threading.Tasks;

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

        [QueuedTask(Name = "SubstractTask")]
        public int Substract(int right, int left) => right - left;

        [QueuedTask(Name = "DelayWrite")]
        public async Task DelayWrite(string message, int seconds = 0)
        {
            await Task.Delay(seconds * 1000);
            Console.WriteLine($"After {seconds} seconds: {message}");
        }
    }
}
