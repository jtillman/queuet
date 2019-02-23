using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AspNetCoreWebApp.FileAnalyzer
{
    [Route("api/fileanalysis")]
    [ApiController]
    public class FileAnalysisController : ControllerBase
    {
        private readonly FileAnalyzerService _fileAnalyzerService;

        public FileAnalysisController(
            FileAnalyzerService fileAnalyzerService)
        {
            _fileAnalyzerService = fileAnalyzerService;
        }

        [HttpGet("{fileId}/{hash}/")]
        public async Task<ActionResult> GetFileAnalysisAsync(string fileId, string hash)
        {
            var analysis = await _fileAnalyzerService.GetByFileIdAsync(fileId, hash);
            if (null == analysis)
            {
                return NotFound();
            }
            return Ok(analysis.Results);
        }
    }
}