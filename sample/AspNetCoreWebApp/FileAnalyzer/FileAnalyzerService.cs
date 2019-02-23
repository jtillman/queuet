using AspNetCoreWebApp.FileEditor;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QueueT.Notifications;
using QueueT.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreWebApp.FileAnalyzer
{

    public class FileAnalyzerService
    {
        public const string WorkerQueue = "file-analyzer";

        private readonly FileSystemContext _dbContext;

        private readonly FileEditService _fileEditService;

        private readonly INotificationService _notificationService;

        public FileAnalyzerService(
            FileSystemContext dbContext,
            FileEditService fileEditService,
            INotificationService notificationService)
        {
            _dbContext = dbContext;
            _fileEditService = fileEditService;
            _notificationService = notificationService;
        }

        public async Task<FileAnalysis> GetByFileIdAsync(string fileId, string fileHash)
        {
            return await _dbContext.FileAnalysis.SingleOrDefaultAsync(fa => fa.FileId == fileId && fa.Hash == fileHash);
        }

        [QueuedTask(Queue = WorkerQueue)]
        [Subscription(FileEditNotifications.Created)]
        [Subscription(FileEditNotifications.Updated)]
        public async Task OnFileEditChangesRunFileAnalysis(FileEditNotification notification)
        {
            var getFileEditResponse = await _fileEditService.GetByFileIdAsync(notification.FileId, notification.Hash);
            if (getFileEditResponse.HasError)
            {
                // File edit has been changed again and has sent another notification
                return;
            }

            var fileEdit = getFileEditResponse.Result;
            var diagnosticItems = await _GetDiagnosticsForContent(fileEdit.Content);

            var results = JsonConvert.SerializeObject(diagnosticItems
                .Select(d => FileAnalysisEntry.FromDiagnostic(d))
                .ToList());
            await _UpdateFileAnalysisAsync(notification.FileId, notification.Hash, results);
        }

        [QueuedTask(Queue = WorkerQueue)]
        [Subscription(FileEditNotifications.Deleted)]
        public async Task OnFileEditDeleteRemoveFileAnalysis(FileEditNotification notification)
        {
            var fileAnalysis = await GetByFileIdAsync(notification.FileId, notification.Hash);
            _dbContext.Remove(fileAnalysis);
            await _dbContext.SaveChangesAsync();
        }

        private static async Task<List<Microsoft.CodeAnalysis.Diagnostic>> _GetDiagnosticsForContent(string content)
        {
            var tree = CSharpSyntaxTree.ParseText(content);
            var root = (CompilationUnitSyntax)await tree.GetRootAsync();
            return root
                .ChildNodes()
                .Select(node => node.GetDiagnostics().ToList())
                .Aggregate((list1, list2) => list1.Concat(list2).ToList());
        }

        private async Task _UpdateFileAnalysisAsync(string fileId, string hash, string results)
        {
            var analysis = await _dbContext.FileAnalysis.SingleOrDefaultAsync(fa => fa.FileId == fileId);
            if (null == analysis)
            {
                analysis = new FileAnalysis { FileId = fileId, Hash = hash, Results = results };
                await _dbContext.AddAsync(analysis);
            }
            else
            {
                analysis.Hash = hash;
                analysis.Results = results;
            }
            await _dbContext.SaveChangesAsync();
            await _notificationService.NotifyAsync<FileAnalyzerNotification>(
                FileAnalyzerNotifications.Completed,
                new FileAnalyzerNotification
                {
                    FileId = fileId,
                    Hash = hash
                });
        }
    }
}
