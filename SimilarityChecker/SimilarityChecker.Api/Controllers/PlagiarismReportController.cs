using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SimilarityChecker.UI.Services;
using System;
using System.Threading.Tasks;

namespace SimilarityChecker.UI.Controllers
{
    [ApiController]
    public class PlagiarismReportController : ControllerBase
    {
        private readonly IPlagiarismService _service;

        public PlagiarismReportController(IPlagiarismService service)
        {
            _service = service;
        }

        [HttpGet("/api/plagiarism/report/{runId:guid}")]
        public async Task<IActionResult> DownloadReport(Guid runId)
        {
            var result = await _service.GetResultAsync(runId);
            if (result is null)
                return NotFound("Raport inexistent pentru acest runId.");

            var pdf = GeneratePdf(result);

            var fileName = $"Raport_Plagiat_{runId}.pdf";
            return File(pdf, "application/pdf", fileName);
        }

        private static byte[] GeneratePdf(PlagiarismResultDto result)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("Raport verificare plagiat")
                        .FontSize(18).SemiBold();

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text($"Scor similaritate total: {result.OverallSimilarity}%");
                        col.Item().Text($"Prag: {result.Threshold}%");

                        col.Item().LineHorizontal(1);

                        col.Item().Text("Potriviri:");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(1);
                                c.RelativeColumn(3);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("Sursă").SemiBold();
                                h.Cell().Text("Sim.").SemiBold();
                                h.Cell().Text("Observații").SemiBold();
                            });

                            foreach (var m in result.Matches)
                            {
                                table.Cell().Text(m.SourceName);
                                table.Cell().Text($"{m.Similarity}%");
                                table.Cell().Text(m.Note);
                            }
                        });
                    });

                    // Use the string overload of Text so it returns a descriptor and you can chain FontSize(...)
                    page.Footer()
                        .AlignCenter()
                        .Text($"Generat automat • {DateTime.Now:dd.MM.yyyy HH:mm}")
                        .FontSize(9);
                });
            }).GeneratePdf();
        }
    }
}
