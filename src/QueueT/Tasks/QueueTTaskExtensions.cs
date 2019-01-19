using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace QueueT.Tasks
{
    public static class QueueTTaskExtensions
    {
        public static string GetDefaultTaskNameForMethod(this MethodInfo method)
        {
            return $"{method.DeclaringType.Namespace}.{method.DeclaringType.Name}.{method.Name}";
        }

        public static IEnumerable<KeyValuePair<MethodInfo, QueuedTaskAttribute>> GetQueuedTaskMethods(this Assembly assembly)
        {
            return assembly.GetTypes()
                           .SelectMany(t => t.GetMethods())
                           .Select(m => new KeyValuePair<MethodInfo, QueuedTaskAttribute>(m, m.GetCustomAttribute<QueuedTaskAttribute>()))
                           .Where(kvp => kvp.Value != null);
        }

        public static object[] GetArgumentArray(this MethodInfo methodInfo, IDictionary<string, object> arguments)
        {
            return methodInfo.GetParameters()
                .Select(param =>
                {
                    if (arguments.ContainsKey(param.Name))
                        return arguments[param.Name];
                    if (param.IsOptional)
                        return param.DefaultValue;
                    throw new ArgumentException($"QueuedTask for [{methodInfo.GetDefaultTaskNameForMethod()}] is missing Argument for [{param.Name}]");
                }).ToArray();
        }
    }
}
