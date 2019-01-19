using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace QueueT.Tasks
{
    public static class QueueTTaskServiceCollectionExtensions
    {
        public static IServiceCollection AddQueueTTasks(this IServiceCollection services, IQueueTBroker queueBroker)
        {
            // var connection = new ServiceBusConnection("string");
            // var queueTServiceBusBroker = new QueueTServiceBusBroker("string");
            // services.AddQueueTTasks(servicesBusBroker)
            //  
            // services.UseQueueTTasks(new ServiceBusBroker
            return services;
        }
    }
}
