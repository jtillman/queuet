using System.Reflection;

namespace QueueT
{
    public class MethodHandler
    {
        public MethodInfo Method { get; }

        public ParameterInfo[] Parameters { get; }

        public MethodHandler(MethodInfo methodInfo)
        {
            Method = methodInfo;
            Parameters = Method.GetParameters();
        }
    }
}
