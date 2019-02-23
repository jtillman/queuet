using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreWebApp.Files
{

    [Route("api/files")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly FileService _fileService;

        public FilesController(FileService fileService)
        {
            _fileService = fileService;
        }

        private object ToJsonObject(File file)
        {
            return new
            {
                id = file.Id,
                content = file.Content,
                name = file.Name
            };
        }

        [HttpGet("")]
        public async Task<ActionResult> CursorSearchFilesAsync(
            int maxLength = 5,
            string nameGt = null)
        {
            var results = await _fileService.SearchFilesAsync(maxLength, nameGt);

            return Ok(new {
                items = results.Select(r=>ToJsonObject(r)).ToList()
            });
        }

        public class CreateFileRequest
        {
            public string Name { get; set; }

            public string Content { get; set; }
        }

        [HttpPost("")]
        public async Task<ActionResult> CreateFileAsync([FromBody] CreateFileRequest createRequest)
        {
            var response = await _fileService.CreateAsync(createRequest.Name, createRequest.Content);
            if (response.HasError)
                return BadRequest();
            return Created(response.Result.Id, ToJsonObject(response.Result));
        }
    }
}