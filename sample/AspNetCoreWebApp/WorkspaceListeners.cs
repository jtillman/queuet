using AspNetCoreWebApp.FileAnalyzer;
using Microsoft.AspNetCore.SignalR;
using QueueT.Notifications;
using QueueT.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreWebApp
{
    public class WorkspaceListeners
    {
        public const string WorkerQueue = "signalR";

        private readonly FileAnalyzerService _fileAnalyzerService;

        private readonly IHubContext<WorkspaceHub,IWorkspaceHubClient> _workspaceHub;

        public WorkspaceListeners(
            FileAnalyzerService fileAnalyzerService,
            IHubContext<WorkspaceHub, IWorkspaceHubClient> workspaceHub)
        {
            _fileAnalyzerService = fileAnalyzerService;
            _workspaceHub = workspaceHub;
        }

        [QueuedTask(Queue = WorkerQueue)]
        [Subscription(FileAnalyzerNotifications.Completed)]
        public async Task OnFileAnalyzerCompleted(FileAnalyzerNotification notification)
        {
            var result = await _fileAnalyzerService.GetByFileIdAsync(notification.FileId, notification.Hash);
            if (null == result)
            {
                return;
            }

            await _workspaceHub.Clients.All.FileStatusChanged(notification.FileId, notification.Hash );
        }
    }
}
