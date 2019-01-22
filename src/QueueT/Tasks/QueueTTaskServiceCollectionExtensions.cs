using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Reflection;

namespace QueueT.Tasks
{

    public static class QueueTTaskServiceCollectionExtensions
    {
        public static IServiceCollection AddQueueTTasks(this IServiceCollection services, IQueueTBroker queueBroker, string defaultQueueName = null)
        {
            services = services.AddSingleton<IQueueTTaskService>((s) =>
            {
                var taskService = new QueueTTaskService(
                    s.GetRequiredService<ILogger<QueueTTaskService>>(),
                    s,
                    queueBroker,
                    s.GetService<IOptions<QueueTTaskOptions>>());

                return taskService;
            });

            services.Configure<QueueTTaskOptions>(config =>
            {
                config.DefaultQueueName = defaultQueueName;
            });

            return services;
        }

        public static IServiceCollection RegisterQueuedTaskAttibutes(this IServiceCollection services, Assembly assembly)
        {
            services.Configure<QueueTTaskOptions>(config =>
            {
                assembly.GetTypes()
                .SelectMany(t => t.GetMethods())
                .Select(m => new { method = m, attribute = m.GetCustomAttribute<QueuedTaskAttribute>() })
                .Where(entry => entry.attribute != null)
                .ToList()
                .ForEach(entry =>
                {
                    var taskName = entry.attribute.TaskName ?? entry.method.GetDefaultTaskNameForMethod();
                    var queueName = entry.attribute.QueueName ?? config.DefaultQueueName;
                    config.Tasks.Add(new TaskDefinition(taskName, entry.method, queueName));
                });
            });
            return services;
        } 
    }
}
