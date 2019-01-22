using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace QueueT.Tasks
{
    public class TaskDefinition
    {
        public string Name { get; }

        public MethodInfo Method { get; }

        public string QueueName { get; }

        public ParameterInfo[] Parameters { get; }

        public TaskDefinition(string taskName, MethodInfo method, string queueName)
        {
            if (string.IsNullOrWhiteSpace(taskName))
            {
                throw new ArgumentException("message", nameof(taskName));
            }

            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException("message", nameof(queueName));
            }

            Name = taskName.Trim();
            Method = method ?? throw new ArgumentNullException(nameof(method));
            QueueName = queueName.Trim();
            Parameters = method.GetParameters();
        }

        public IDictionary<string, object> CreateArgumentsFromCall(MethodCallExpression expression)
        {
            var parameterValues = new Dictionary<string, object>();
            for (int i = 0; i < Parameters.Length; i++)
            {
                var objectValue = Expression.Lambda(expression.Arguments[i]).Compile().DynamicInvoke();
                parameterValues[Parameters[i].Name] = objectValue;
            }
            return parameterValues;
        }

        public object[] GetParametersFromArguments(IDictionary<string, object> taskArguments)
        {
            var argumentList = new List<object>(Parameters.Length);
            var missingArguments = new List<string>();

            foreach (var paramInfo in Parameters)
            {
                if (taskArguments.TryGetValue(paramInfo.Name, out var value))
                    argumentList.Add(value);
                else if (paramInfo.IsOptional)
                    argumentList.Add(paramInfo.DefaultValue);
                else
                    missingArguments.Add(paramInfo.Name);
            }

            if (0 < missingArguments.Count)
                throw new ArgumentException($"Message for task [{Name}] missing arguments: {string.Join(", ", missingArguments)}");

            return argumentList.ToArray();
        }
    }
}
