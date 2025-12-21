using Microsoft.AspNetCore.Mvc;
using VideoManager.Models;

namespace VideoManager.Controllers
{
    [ApiController]
    [Route("api/export_single")]
    public class ExportSingleController : ControllerBase
    {
        private readonly IExportSingleRealmService _exportService;

        public ExportSingleController(IExportSingleRealmService exportService)
        {
            _exportService = exportService;
        }

        [HttpGet]
        public async Task<IActionResult> Download([FromQuery] string realm, [FromQuery] string view)
        {
            var zip = await _exportService.BuildSingleZipAsync(realm, view);
            return File(zip, "application/zip", "export.zip");
        }
    }

    [ApiController]
    [Route("api/export_all")]
    public class ExportAllController : ControllerBase
    {
        private readonly IExportAllRealmService _exportService;

        public ExportAllController(IExportAllRealmService exportService)
        {
            _exportService = exportService;
        }

        [HttpGet]
        public async Task<IActionResult> Download()
        {
            var zip = await _exportService.BuildAllZipAsync();
            return File(zip, "application/zip", "export.zip");
        }
    }
}
