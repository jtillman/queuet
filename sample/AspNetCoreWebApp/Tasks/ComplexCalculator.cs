using QueueT.Tasks;

namespace AspNetCoreWebApp.Tasks
{
    public class ComplexCalculator
    {
        [QueuedTask(TaskName = "Multiply")]
        public int Multiply(int right, int left) => right * left;

        [QueuedTask(TaskName = "Add")]
        public int Add(int right, int left) => right + left;

        [QueuedTask(TaskName = "Substract")]
        public int Substract(int right, int left) => right - left;
    }
}
