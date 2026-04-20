using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SimilarityChecker.Api.Models;
using SimilarityChecker.Api.Services.InternalScan;
using InternalScanReportDto = SimilarityChecker.Shared.Dto.InternalScanReportDto;
using FragmentTypeDto = SimilarityChecker.Shared.Dto.FragmentTypeDto;

namespace SimilarityChecker.Api.Controllers;

[ApiController]
[Route("api/internal-scan")]
public sealed class InternalScanController : ControllerBase
{
    private readonly IInternalScanService _service;

    public InternalScanController(IInternalScanService service) => _service = service;

    [HttpPost("start")]
    public async Task<ActionResult<InternalScanStartResponse>> Start(
        [FromQuery] Guid documentId,
        CancellationToken ct = default)
    {
        const double threshold = 0.25;
        var result = await _service.StartAsync(documentId, threshold, ct);
        return Ok(result);
    }

    [HttpPost("compare")]
    public async Task<ActionResult<InternalScanReportDto>> Compare(
        [FromQuery] Guid primaryDocumentId,
        [FromQuery] Guid referenceDocumentId,
        CancellationToken ct = default)
    {
        const double threshold = 0.25;
        var result = await _service.CompareTwoDocumentsAsync(primaryDocumentId, referenceDocumentId, threshold, ct);
        return Ok(result);
    }

    [HttpGet("report/{documentId:guid}")]
    public async Task<ActionResult<InternalScanReportDto>> GetReport(Guid documentId, CancellationToken ct)
    {
        var report = await _service.GetReportAsync(documentId, ct);
        if (report is null)
            return NotFound("Nu există raport pentru acest document.");

        return Ok(report);
    }

    [HttpGet("report/{documentId:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid documentId, CancellationToken ct)
    {
        var report = await _service.GetReportAsync(documentId, ct);
        if (report is null)
            return NotFound("Nu există raport pentru acest document.");

        var pdfBytes = BuildPdf(
            report,
            "Raport de analiză a similitudinii",
            "Scanare internă",
            $"Document analizat: {report.DocumentId}");

        var fileName = $"raport-scanare-interna-{documentId}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    [HttpGet("report/compare/{primaryDocumentId:guid}/{referenceDocumentId:guid}/pdf")]
    public async Task<IActionResult> DownloadComparePdf(Guid primaryDocumentId, Guid referenceDocumentId, CancellationToken ct)
    {
        const double threshold = 0.25;
        var report = await _service.CompareTwoDocumentsAsync(primaryDocumentId, referenceDocumentId, threshold, ct);

        var pdfBytes = BuildPdf(
            report,
            "Raport de comparație între documente",
            "Comparare document la document",
            $"Document analizat: {primaryDocumentId} | Document de referință: {referenceDocumentId}");

        var fileName = $"raport-comparatie-{primaryDocumentId}-{referenceDocumentId}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    private static byte[] BuildPdf(
        InternalScanReportDto report,
        string title,
        string subtitle,
        string documentInfo)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        int suspiciousTotal = Math.Clamp(report.ExactPercent + report.ParaphrasePercent, 0, 100);
        int exactPct = Math.Clamp(report.ExactPercent, 0, 100);
        int paraPct = Math.Clamp(report.ParaphrasePercent, 0, 100);
        int cleanPct = Math.Clamp(report.CleanPercent, 0, 100);

        if (exactPct == 0 && paraPct == 0 && cleanPct == 0)
        {
            suspiciousTotal = report.Hits.Count == 0
                ? 0
                : (int)Math.Round(report.Hits.Max(x => x.SimilarityScore) * 100);

            exactPct = suspiciousTotal;
            paraPct = 0;
            cleanPct = Math.Max(0, 100 - suspiciousTotal);
        }

        string LevelLabel(int p) => p >= 70 ? "Nivel ridicat" : p >= 30 ? "Nivel mediu" : "Nivel scăzut";

        var groupedFragments = report.Fragments?
            .OrderByDescending(x => x.Type)
            .ThenByDescending(x => x.Score)
            .Take(30)
            .GroupBy(x => x.ReferenceFileName)
            .ToList() ?? new List<IGrouping<string, SimilarityChecker.Shared.Dto.InternalScanFragmentDto>>();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Spacing(4);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text(title)
                                .FontSize(18)
                                .SemiBold();

                            left.Item().Text(subtitle)
                                .FontSize(12)
                                .FontColor(Colors.Grey.Darken2);
                        });

                        row.ConstantItem(220).AlignRight().Column(right =>
                        {
                            right.Item().Text($"Generat: {report.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss} UTC")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken2);

                            right.Item().Text($"Raport ID: {report.ReportId}")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Darken2);
                        });
                    });

                    col.Item().Text(documentInfo)
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken2);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(14);

                    // 1. Rezumat clar
                    col.Item().Text("1. Rezumat")
                        .FontSize(13)
                        .SemiBold();

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(c =>
                        {
                            c.Spacing(6);

                            c.Item().Text("Rezultatul final").SemiBold();
                            c.Item().Text($"Conținut suspect total: {suspiciousTotal}%");
                            c.Item().Text($"Nivel estimat: {LevelLabel(suspiciousTotal)}");
                            c.Item().Text($"Documente comparate: {report.Hits.Count}");

                            c.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            c.Item().Text("Distribuția conținutului").SemiBold();
                            c.Item().Text($"Copiat exact: {exactPct}%");
                            c.Item().Text($"Parafrazat: {paraPct}%");
                            c.Item().Text($"Conținut original estimat: {cleanPct}%");
                        });

                        row.ConstantItem(220).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(c =>
                        {
                            c.Spacing(10);
                            c.Item().Text("Vizual").SemiBold();

                            c.Item().Row(r =>
                            {
                                if (exactPct > 0)
                                    r.RelativeItem(exactPct).Height(14).Background(Colors.Red.Medium);

                                if (paraPct > 0)
                                    r.RelativeItem(paraPct).Height(14).Background(Colors.Orange.Medium);

                                if (cleanPct > 0)
                                    r.RelativeItem(cleanPct).Height(14).Background(Colors.Green.Medium);

                                if (exactPct == 0 && paraPct == 0 && cleanPct == 0)
                                    r.RelativeItem(1).Height(14).Background(Colors.Grey.Lighten2);
                            });

                            c.Item().Text("Legendă")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken2);

                            c.Item().Row(r =>
                            {
                                r.ConstantItem(10).Height(10).Background(Colors.Red.Medium);
                                r.ConstantItem(6);
                                r.RelativeItem().Text("Copiat exact").FontSize(10);
                            });

                            c.Item().Row(r =>
                            {
                                r.ConstantItem(10).Height(10).Background(Colors.Orange.Medium);
                                r.ConstantItem(6);
                                r.RelativeItem().Text("Parafrazat").FontSize(10);
                            });

                            c.Item().Row(r =>
                            {
                                r.ConstantItem(10).Height(10).Background(Colors.Green.Medium);
                                r.ConstantItem(6);
                                r.RelativeItem().Text("Original estimat").FontSize(10);
                            });
                        });
                    });

                    // 2. Surse comparate
                    col.Item().Text("2. Documente comparate")
                        .FontSize(13)
                        .SemiBold();

                    if (report.Hits.Count == 0)
                    {
                        col.Item().Text("Nu au fost găsite documente similare peste pragul stabilit.")
                            .FontColor(Colors.Grey.Darken2);
                    }
                    else
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(6);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(6)
                                    .Text("Document de referință").SemiBold();

                                header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight()
                                    .Text("Scor comparativ").SemiBold();
                            });

                            foreach (var hit in report.Hits.OrderByDescending(x => x.SimilarityScore).Take(10))
                            {
                                var p = (int)Math.Round(hit.SimilarityScore * 100);

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6)
                                    .Text(hit.ComparedFileName);

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).AlignRight()
                                    .Text($"{p}%");
                            }
                        });
                    }

                    // 3. Fragmente
                    col.Item().Text("3. Fragmente identificate")
                        .FontSize(13)
                        .SemiBold();

                    if (report.Fragments is null || report.Fragments.Count == 0)
                    {
                        col.Item().Text("Nu au fost identificate fragmente similare relevante.")
                            .FontColor(Colors.Grey.Darken2);
                    }
                    else
                    {
                        foreach (var group in groupedFragments)
                        {
                            col.Item().PaddingTop(4).Text($"Sursă: {group.Key}")
                                .SemiBold()
                                .FontColor(Colors.Blue.Darken2);

                            foreach (var f in group)
                            {
                                var label = f.Type == FragmentTypeDto.Exact ? "Copiat exact" : "Parafrazat";
                                var pct = (int)Math.Round(f.Score * 100);

                                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(box =>
                                {
                                    box.Spacing(6);

                                    box.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text($"{label} • {pct}%")
                                            .SemiBold()
                                            .FontColor(f.Type == FragmentTypeDto.Exact
                                                ? Colors.Red.Darken2
                                                : Colors.Orange.Darken2);
                                    });

                                    box.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                                    box.Item().Text("Fragment din documentul analizat:")
                                        .FontSize(10)
                                        .FontColor(Colors.Grey.Darken2);

                                    box.Item().Text(f.SourceSnippet)
                                        .FontSize(10);

                                    box.Item().Text("Fragment din documentul de referință:")
                                        .FontSize(10)
                                        .FontColor(Colors.Grey.Darken2);

                                    box.Item().Text(f.ReferenceSnippet)
                                        .FontSize(10);
                                });
                            }
                        }

                        col.Item().Text("*Se afișează maxim 30 fragmente.")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken2);
                    }
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem()
                        .Text("SimilarityChecker • Raport generat automat")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken2);

                    row.ConstantItem(120).AlignRight()
                        .Text(txt =>
                        {
                            txt.Span("Pagina ").FontSize(9).FontColor(Colors.Grey.Darken2);
                            txt.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Darken2);
                            txt.Span(" / ").FontSize(9).FontColor(Colors.Grey.Darken2);
                            txt.TotalPages().FontSize(9).FontColor(Colors.Grey.Darken2);
                        });
                });
            });
        });

        return doc.GeneratePdf();
    }
}