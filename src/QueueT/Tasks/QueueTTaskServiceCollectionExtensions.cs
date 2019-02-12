﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace QueueT.Tasks
{

    public static class QueueTTaskServiceCollectionExtensions
    {
        public static QueueTServiceCollection UseTasks(this QueueTServiceCollection services, Action<TaskOptions> configure = null)
        {
            services.Services.AddOptions<QueueTServiceOptions>();
            services.AddQueueTMessageHandler<TaskService>();
            services.Services.AddSingleton<ITaskService>(sp => sp.GetRequiredService<TaskService>());

            if (null != configure)
            {
                services.Services.Configure<TaskOptions>(config => configure(config));
            }

            return services;
        }

        public static QueueTServiceCollection AddQueueTMessageHandler<T>(this QueueTServiceCollection services) where T : class, IMessageHandler
        {
            services.Services.AddSingleton<T>();
            services.Services.Configure<QueueTServiceOptions>(options => options.RegisterHandlerType<T>());
            return services;
        }

        public static void RegisterTaskAttibutes(this TaskOptions options, Assembly assembly)
        {
            assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Select(m => new { method = m, attribute = m.GetCustomAttribute<QueuedTaskAttribute>() })
            .Where(entry => entry.attribute != null)
            .ToList()
            .ForEach(entry =>
            {
                var taskName = entry.attribute.TaskName ?? entry.method.GetDefaultTaskNameForMethod();
                var queueName = entry.attribute.QueueName ?? options.DefaultQueueName;
                options.Tasks.Add(new TaskDefinition(taskName, entry.method, queueName));
            });
        } 
    }
}
