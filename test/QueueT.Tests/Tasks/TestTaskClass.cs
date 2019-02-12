using System.Threading.Tasks;

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
}