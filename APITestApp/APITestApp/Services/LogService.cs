using Nest;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using APITestApp.Models;

namespace APITestApp.Services
{
    public class LogService
    {
        private readonly IElasticClient _client;

        public LogService(IConfiguration configuration)
        {

            var url = configuration["Elasticsearch:Url"];
            var user = configuration["Elasticsearch:Username"];
            var pass = configuration["Elasticsearch:Password"];

            var settings = new ConnectionSettings(new Uri(url))
                .BasicAuthentication(user, pass)
                .DefaultIndex("api_logs")
                .ServerCertificateValidationCallback((a, b, c, d) => true); 

            _client = new ElasticClient(settings);
        }

        public async Task<string> TestConnectionAsync()
        {
            var ping = await _client.PingAsync();
            if (ping.IsValid)
                return "Connected to Elasticsearch!";
            return $"Failed to connect: {ping.OriginalException?.Message}";
        }

        
        public async Task<List<Dictionary<string, object>>> GetLogsAsync()
        {
            var response = await _client.SearchAsync<object>(s => s
                .Index("api_logs")
                .Size(50)
                .Source(sf => sf.Includes(f => f
                    .Fields("api_name", "status", "response_time", "error", "timestamp")
                ))
            );

            if (!response.IsValid)
                throw new Exception($"Elasticsearch error: {response.OriginalException?.Message}");

            var logsWithId = new List<Dictionary<string, object>>();

            foreach (var hit in response.Hits)
            {
                var dict = hit.Source as IDictionary<string, object>;
                var dictCopy = dict != null
                    ? dict.ToDictionary(k => k.Key, v => v.Value)
                    : new Dictionary<string, object>();

                dictCopy["_id"] = hit.Id;
                logsWithId.Add(dictCopy);
            }

            return logsWithId;
        }


        public async Task<byte[]> GeneratePdfAsync(IEnumerable<object> logs)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var logList = logs.Cast<Dictionary<string, object>>().ToList();

            var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12).FontColor(Colors.Black));

                    page.Header()
                        .Column(header =>
                        {
                            header.Item().Text("API Logs Report")
                                .FontSize(24).SemiBold().FontColor(Colors.Blue.Medium);

                            header.Item().Text($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                                .FontSize(10).FontColor(Colors.Grey.Darken1);

                            header.Item().PaddingTop(10).Text(
                                "This report provides a detailed overview of recent API requests, " +
                                "including their status, response times, and any errors encountered. " +
                                "It is intended to help monitor API performance, identify failures, " +
                                "and provide actionable insights for debugging and optimization."
                            ).FontSize(11).FontColor(Colors.Grey.Darken2);
                        });

                    page.Content()
                        .PaddingTop(20)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); 
                                columns.RelativeColumn(1); 
                                columns.RelativeColumn(2); 
                                columns.RelativeColumn(4); 
                                columns.RelativeColumn(2); 
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Text("API Name").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten2).Text("Status").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten2).Text("Response Time").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten2).Text("Error").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten2).Text("Timestamp").SemiBold();
                            });

                            bool alternate = false;
                            foreach (var log in logList)
                            {
                                var rowColor = alternate ? Colors.Grey.Lighten5 : Colors.White;
                                alternate = !alternate;

                                table.Cell().Background(rowColor).Text(log.GetValueOrDefault("api_name")?.ToString() ?? "-");
                                table.Cell().Background(rowColor).Text(log.GetValueOrDefault("status")?.ToString() ?? "-");
                                table.Cell().Background(rowColor).Text(log.GetValueOrDefault("response_time")?.ToString() ?? "-");
                                table.Cell().Background(rowColor).Text(log.GetValueOrDefault("error")?.ToString() ?? "-");
                                table.Cell().Background(rowColor).Text(log.GetValueOrDefault("timestamp")?.ToString() ?? "-");
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text("© FINKI – API Monitoring System")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            }).GeneratePdf();

            return await Task.FromResult(pdfBytes);
        }

    }
}
