using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QueueT;
using QueueT.Tasks;
using QueueT.Notifications;
using System.Reflection;
using AspNetCoreWebApp.Files;
using AspNetCoreWebApp.FileEditor;
using AspNetCoreWebApp.FileAnalyzer;

namespace AspNetCoreWebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddScoped<FileService>()
                .AddScoped<FileEditService>()
                .AddScoped<FileAnalyzerService>();

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            var defaultQueue = "default";
            var notificationsQueue = "notifications";

            var queueNames = new string[] {
                defaultQueue,
                FileAnalyzerService.WorkerQueue,
                FileEditService.WorkerQueue,
                WorkspaceListeners.WorkerQueue,
                notificationsQueue};

            services.AddQueueT(config => {
                config.Broker = new QueueT.Brokers.InMemoryBroker(queueNames);
                config.WorkerTaskCount = 1;
                config.DefaultQueueName = defaultQueue;
                config.AddQueues(queueNames);
            })
            .ConfigureNotifications(options =>
            {
                options.DefaultQueueName = notificationsQueue;
                options.RegisterNotificationAttributes();
                options.RegisterSubscriptionAttributes();
            })
            .UseTasks(config => {
                config.DefaultQueueName = queueNames[0];
                config.RegisterTaskAttibutes(Assembly.GetExecutingAssembly());
            });

            services.AddDbContext<FileSystemContext>(options =>
            {
                options.UseInMemoryDatabase();
            });

            services.AddQueueTWorker();
            services.AddSignalR();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc();
            app.UseSignalR(routes => routes.MapHub<WorkspaceHub>("/hubs/workspace"));
        }
    }
}
