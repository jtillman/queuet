using QueueT.Tasks;

namespace AspNetCoreWebApp.Tasks
{
    public class ComplexCalculator
    {
        [QueuedTask]
        public int Multiply(int right, int left) => right * left;

        [QueuedTask]
        public int Add(int right, int left) => right + left;

        [QueuedTask(TaskName = "SubstractTask")]
        public int Substract(int right, int left) => right - left;
    }
}
