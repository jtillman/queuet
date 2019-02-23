using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreWebApp.FileEditor
{
    [Route("api/fileedits")]
    [ApiController]
    public class FileEditsController: ControllerBase
    {

        private readonly FileEditService _fileEditService;

        public FileEditsController(FileEditService fileEditService)
        {
            _fileEditService = fileEditService;
        }

        [HttpGet("")]
        public async Task<ActionResult> GetFileChanges()
        {
            var results = await _fileEditService.SearchAsync();

            return Ok(new
            {
                items = results.Select(fc => ToJsonObject(fc)).ToArray()
            });
        }

        public class CreateFileChangeRequest
        {
            public string FileId { get; set; }
        }

        [HttpPost("")]
        public async Task<ActionResult> CreateFileChange([FromBody] CreateFileChangeRequest request)
        {
            var response = await _fileEditService.CreateAsync(request.FileId);
            if (response.HasError)
            {
                return BadRequest();
            }
            return Created("", ToJsonObject(response.Result));
        }

        public class PatchFileChangeRequest
        {
            public string Content { get; set; }
        }

        [HttpPatch("{fileId}/{hash}/")]
        public async Task<ActionResult> UpdateFileChange(string fileId, string hash, [FromBody] PatchFileChangeRequest request)
        {
            var response = await _fileEditService.UpdateContentAsync(fileId, hash, request.Content);
            if (response.HasError)
            {
                return BadRequest();
            }
            return Ok(ToJsonObject(response.Result));
        }

        [HttpPost("{fileId}/{hash}/commit")]
        public async Task<ActionResult> CommitFileChange(string fileId, string hash)
        {
            var response = await _fileEditService.CommitChangesAsync(fileId, hash);
            if (response.HasError)
            {
                return BadRequest();
            }

            return Created("", ToJsonObject(response.Result));
        }

        [HttpDelete("{fileId}/{hash}/")]
        public async Task<ActionResult> DeleteFileChange(string fileId)
        {
            var response = await _fileEditService.DeleteAsync(fileId);
            if (response.HasError)
            {
                return BadRequest();
            }
            return NoContent();
        }

        public static object ToJsonObject(FileEdit fileEdit) => new
        {
            fileId = fileEdit.FileId,
            fileName = fileEdit.FileName,
            content = fileEdit.Content,
            hash = fileEdit.Hash
        };
    }
}