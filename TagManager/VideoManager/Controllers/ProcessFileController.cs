using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;


namespace VideoManager.Controllers
{
    [ApiController]
    // [Route("api/[controller]")]
    [Route("api/[controller]")]
    public class ProcessFileController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IAntiforgery _antiforgery;

        public ProcessFileController(IConfiguration config, IAntiforgery antiforgery)
        {
            _config = config;
            _antiforgery = antiforgery;
        }

        /*
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] FileRequest request)
        {
            // Validate antiforgery token
            await _antiforgery.ValidateRequestAsync(HttpContext);

            Console.WriteLine($"Received file content: {request.FileContent.Substring(0, Math.Min(50, request.FileContent.Length))}...");
            return Ok(new { Result = "ok" });
        }
        */
        
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] FileRequest request)
        {

            await _antiforgery.ValidateRequestAsync(HttpContext);

            var tempFile = Path.GetTempFileName() + ".txt";
            await System.IO.File.WriteAllTextAsync(tempFile, request.FileContent);
            
            var scriptPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "script.py");
            string connectionString = _config.GetConnectionString("PizzaDB");

            var psi = new ProcessStartInfo
            {
                FileName = "python", // TODO: make sure it's cross-platform (as python3 might be required for some systems)
                Arguments = $"\"{scriptPath}\" \"{tempFile}\" \"{connectionString}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(psi)!;
            var output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();

            return Ok(new { Result = output });
        }
        
    }

    public class FileRequest
    {
        public string FileContent { get; set; } = string.Empty;
    }
}
