using Microsoft.Extensions.DependencyInjection;

namespace QueueT
{
    public class QueueTServiceCollection
    {
        public IServiceCollection Services { get; }

        public QueueTServiceCollection(IServiceCollection services) {
            Services = services;
        }
    }
}
