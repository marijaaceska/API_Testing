using Microsoft.AspNetCore.Mvc;
using APITestApp.Services;

namespace APITestApp.Controllers
{
    public class LogController : Controller
    {
        private readonly LogService _logService;

        public LogController(LogService logService)
        {
            _logService = logService;
        }


        public async Task<IActionResult> Index()
        {
            try
            {
                var logs = await _logService.GetLogsAsync();

                // (newest first)
                var sortedLogs = logs
                    .Cast<Dictionary<string, object>>()   
                    .OrderByDescending(l =>
                    {
                        if (l.TryGetValue("timestamp", out var ts) && ts != null)
                        {
                            return DateTime.TryParse(ts.ToString(), out var dt) ? dt : DateTime.MinValue;
                        }
                        return DateTime.MinValue;
                    })
                    .ToList<object>();

                return View(sortedLogs);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new List<object>());
            }
        }
        public async Task<IActionResult> Test()
        {
            var result = await _logService.TestConnectionAsync();
            ViewBag.Status = result;
            return View();
        }

        public async Task<IActionResult> DownloadPdf()
        {
            var logs = await _logService.GetLogsAsync();
            var pdfBytes = await _logService.GeneratePdfAsync(logs);

            return File(pdfBytes, "application/pdf", "ApiLogs.pdf");
        }
    }
}
