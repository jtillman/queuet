using AspNetCoreWebApp.Files;
using Microsoft.EntityFrameworkCore;
using QueueT.Notifications;
using QueueT.Tasks;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreWebApp.FileEditor
{

    public class FileEditService
    {
        public const string WorkerQueue = "file-edit";

        private readonly FileSystemContext _dbContext;

        private readonly FileService _fileService;

        private readonly ITaskService _taskService;

        private readonly INotificationService _notificationService;

        public FileEditService(
            FileSystemContext dbContext,
            FileService fileService,
            ITaskService taskService,
            INotificationService notificationService)
        {
            _dbContext = dbContext;
            _fileService = fileService;
            _taskService = taskService;
            _notificationService = notificationService;
        }

        private async Task _NotifyAsync(FileEdit fileEdit, FileEditNotifications notification)
        {
            await _notificationService.NotifyAsync(
                notification,
                new FileEditNotification
                {
                    FileId = fileEdit.FileId,
                    Hash = fileEdit.Hash
                });
        }

        public async Task<FileEdit[]> SearchAsync(int maxResults = 50)
        {
            return await _dbContext.FileEdits.Take(maxResults).ToArrayAsync();
        }

        public async Task<Response<FileEdit, FileEditErrors>> CreateAsync(string fileId)
        {
            if (await _dbContext.FileEdits.AnyAsync(fe => fe.FileId == fileId))
            {
                return new Response<FileEdit, FileEditErrors>(FileEditErrors.FileEditAlreadyExist);
            }

            var fileResponse = await _fileService.GetByIdAsync(fileId);
            if (fileResponse.HasError)
            {
                return new Response<FileEdit, FileEditErrors>(FileEditErrors.FileEditConflict);
            }

            var file = fileResponse.Result;

            var newFileEdit = new FileEdit
            {
                FileId = file.Id,
                FileName = file.Name,
                Content = file.Content,
                StartingHash = file.Hash,
                Hash = file.Hash
            };

            await _dbContext.FileEdits.AddAsync(newFileEdit);
            await _dbContext.SaveChangesAsync();
            await _NotifyAsync(newFileEdit, FileEditNotifications.Created);
            return new Response<FileEdit, FileEditErrors>(newFileEdit);
        }

        private async Task<FileEdit> _GetByFileIdAsync(string fileId)
        {
            return await _dbContext.FileEdits.SingleOrDefaultAsync(fe => fe.FileId == fileId);
        }

        public async Task<Response<FileEdit, FileEditErrors>> GetByFileIdAsync(string fileId, string hash = null)
        {
            var fileEdit = await _GetByFileIdAsync(fileId);
            if (null == fileEdit)
            {
                return new Response<FileEdit, FileEditErrors>(FileEditErrors.FileEditDoesNotExist);
            }

            if (null != hash && hash != fileEdit.Hash)
            {
                return new Response<FileEdit, FileEditErrors>(FileEditErrors.FileEditConflict);
            }

            return new Response<FileEdit, FileEditErrors>(fileEdit);
        }

        public async Task<Response<FileEdit, FileEditErrors>> UpdateContentAsync(string fileId, string lastHash, string content)
        {
            var getFileEditResponse = await GetByFileIdAsync(fileId, lastHash);
            if (getFileEditResponse.HasError)
            {
                return getFileEditResponse;
            }

            var fileEdit = getFileEditResponse.Result;

            fileEdit.Content = content;
            fileEdit.Hash = Guid.NewGuid().ToString();

            await _dbContext.SaveChangesAsync();
            await _NotifyAsync(fileEdit, FileEditNotifications.Updated);
            return new Response<FileEdit, FileEditErrors>(fileEdit);
        }

        public async Task<Response<FileEdit, FileEditErrors>> CommitChangesAsync(string fileId, string lastHash)
        {
            var getFileEditResponse = await GetByFileIdAsync(fileId, lastHash);
            if (getFileEditResponse.HasError)
            {
                return getFileEditResponse;
            }

            var fileEdit = getFileEditResponse.Result;

            var updateResponse = await _fileService.UpdateFileAsync(fileId, fileEdit.StartingHash, fileEdit.Content);
            if (updateResponse.HasError)
            {
                return new Response<FileEdit, FileEditErrors>(FileEditErrors.FileEditConflict);
            }

            fileEdit.StartingHash = updateResponse.Result.Hash;
            await _dbContext.SaveChangesAsync();
            await _NotifyAsync(fileEdit, FileEditNotifications.Committed);
            return new Response<FileEdit, FileEditErrors>(fileEdit);
        }

        public async Task<Response<FileEditErrors>> DeleteAsync(string fileId)
        {
            var fileEdit = await _GetByFileIdAsync(fileId);
            if (null == fileEdit)
            {
                return new Response<FileEditErrors>(FileEditErrors.FileEditDoesNotExist);
            }
            _dbContext.Remove(fileEdit);
            await _dbContext.SaveChangesAsync();
            await _NotifyAsync(fileEdit, FileEditNotifications.Deleted);
            return new Response<FileEditErrors>();
        }

        [QueuedTask(Queue = WorkerQueue)]
        [Subscription(FileNotifications.Deleted)]
        public async Task OnFileDeletionAsync(FileNotification notification)
        {
            await DeleteAsync(notification.FileId);
        }
    }
}
