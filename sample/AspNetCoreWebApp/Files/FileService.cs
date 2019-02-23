using Microsoft.EntityFrameworkCore;
using QueueT.Notifications;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreWebApp.Files
{

    public class FileServiceResponse<T>
    {
        public FileServiceErrors? Error { get; }

        public T Result { get; }

        public FileServiceResponse(T result) { Result = result; }

        public FileServiceResponse(FileServiceErrors error) { Error = error; }
    }

    public class FileService
    {
        private readonly FileSystemContext _dbContext;

        private readonly INotificationService _notificationDispatcher;

        public FileService(
            FileSystemContext dbContext,
            INotificationService notificationDispatcher
            )
        {
            _dbContext = dbContext;
            _notificationDispatcher = notificationDispatcher;
        }

        public async Task<Response<File, FileServiceErrors>> GetByIdAsync(string fileId)
        {
            var file = await _dbContext.Files.SingleOrDefaultAsync(f => f.Id == fileId);
            if (null == file) return new Response<File, FileServiceErrors>(FileServiceErrors.FileDoesNotExist);
            return new Response<File, FileServiceErrors>(file);
        }

        public async Task<Response<File, FileServiceErrors>> CreateAsync(string name, string content)
        {
            if(await _dbContext.Files.AnyAsync(f=>string.Compare(f.Name, name, StringComparison.InvariantCultureIgnoreCase) == 0))
            {
                return new Response<File, FileServiceErrors>(FileServiceErrors.FileAlreadyExist);
            }

            var file = new File
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Content = content,
                Hash = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            await _dbContext.Files.AddAsync(file);
            await _dbContext.SaveChangesAsync();

            await _notificationDispatcher.NotifyAsync(
                FileNotifications.Created,
                new FileNotification
                {
                    FileId = file.Id,
                    Hash = file.Hash
                });

            return new Response<File, FileServiceErrors>(file);
        }

        public async Task<File[]> SearchFilesAsync(int maxResults = 10, string nameGt = null)
        {
            var filter = _dbContext.Files.AsQueryable();

            if (null != nameGt)
                filter = filter.Where(f => string.Compare(f.Name, nameGt) > 0);

            return await filter.Take(maxResults).OrderBy(f => f.Name).ToArrayAsync();
        }

        public async Task<Response<File, FileServiceErrors>> UpdateFileAsync(string fileId, string hash, string contents)
        {
            var getFileResponse = await GetByIdAsync(fileId);
            if (getFileResponse.HasError)
            {
                return getFileResponse;
            }

            var file = getFileResponse.Result;
            if (file.Hash != hash)
            {
                return new Response<File, FileServiceErrors>(FileServiceErrors.Unknown);
            }

            file.Content = contents;
            file.Hash = Guid.NewGuid().ToString();
            file.LastModified = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return new Response<File, FileServiceErrors>(file);
        }
    }
}
