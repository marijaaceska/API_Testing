using APITestApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace APITestApp.Controllers
{
    public class EmailController : Controller
    {
        private readonly EmailService _emailService;
        private readonly LogService _logService;

        private const string RecipientEmail = "fakultetmarija@gmail.com";

        public EmailController(EmailService emailService, LogService logService)
        {
            _emailService = emailService;
            _logService = logService;
        }

        [HttpPost]
        public async Task<IActionResult> SendReport()
        {
            try
            {
                var logs = await _logService.GetLogsAsync();

                var pdfBytes = await _logService.GeneratePdfAsync(logs);

                await _emailService.SendEmailAsync(
                    subject: "API Logs Report",
                    body: "Dear Team,\r\n\r\nPlease find attached the latest API logs report. " +
                    "\r\n\r\nThis report provides a comprehensive overview of API performance, " +
                    "including:\r\n- Status of recent API requests\r\n- Response times and latency\r\n- " +
                    "Errors or failed requests\r\n\r\nIt is intended to help monitor system health, identify potential issues, " +
                    "and support timely troubleshooting.\r\n\r\nShould you have any questions or require further details, " +
                    "please do not hesitate to reach out.\r\n\r\nBest regards,\r\nAPI Monitoring System\r\n",
                    attachmentBytes: pdfBytes,
                    attachmentName: "ApiLogs.pdf"
                    );

                TempData["SuccessMessage"] = "Report sent successfully";
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = $"Error sending report: {ex.Message}";
            }

            return RedirectToAction("Index", "Log");
        }
    }
}
