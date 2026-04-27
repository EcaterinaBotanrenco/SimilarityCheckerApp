using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SimilarityChecker.Api.Data;
using SimilarityChecker.Api.Models;
using SimilarityChecker.Api.Services.InternalScan;
using SimilarityChecker.Shared.Dto;
using FragmentTypeDto = SimilarityChecker.Shared.Dto.FragmentTypeDto;
using InternalScanReportDto = SimilarityChecker.Shared.Dto.InternalScanReportDto;

namespace SimilarityChecker.Api.Controllers;

[ApiController]
[Route("api/internal-scan")]
public sealed class InternalScanController : ControllerBase
{
    private readonly IInternalScanService _service;
    private readonly SimilarityCheckerDbContext _db;

    public InternalScanController(
        IInternalScanService service,
        SimilarityCheckerDbContext db)
    {
        _service = service;
        _db = db;
    }

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

        var documentName = await _db.Documents
            .AsNoTracking()
            .Where(x => x.Id == documentId)
            .Select(x => x.FileName)
            .FirstOrDefaultAsync(ct);

        documentName = string.IsNullOrWhiteSpace(documentName)
            ? $"Document_{documentId}"
            : documentName;

        var pdfBytes = BuildPdf(
            report,
            "Raport de analiză a similitudinii",
            "Scanare internă",
            $"Document analizat: {documentName}",
            documentName);

        var fileName = $"Raport_Similitudini_{SanitizeFileNameWithoutExtension(documentName)}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    [HttpGet("report/compare/{primaryDocumentId:guid}/{referenceDocumentId:guid}/pdf")]
    public async Task<IActionResult> DownloadComparePdf(
        Guid primaryDocumentId,
        Guid referenceDocumentId,
        CancellationToken ct)
    {
        const double threshold = 0.25;

        var report = await _service.CompareTwoDocumentsAsync(
            primaryDocumentId,
            referenceDocumentId,
            threshold,
            ct);

        var documents = await _db.Documents
            .AsNoTracking()
            .Where(x => x.Id == primaryDocumentId || x.Id == referenceDocumentId)
            .Select(x => new { x.Id, x.FileName })
            .ToListAsync(ct);

        var primaryName = documents.FirstOrDefault(x => x.Id == primaryDocumentId)?.FileName
            ?? $"Document_{primaryDocumentId}";

        var referenceName = documents.FirstOrDefault(x => x.Id == referenceDocumentId)?.FileName
            ?? $"Document_{referenceDocumentId}";

        var pdfBytes = BuildPdf(
            report,
            "Raport de comparație între documente",
            "Comparare document la document",
            $"Document analizat: {primaryName} | Document de referință: {referenceName}",
            primaryName);

        var fileName =
            $"Raport_Similitudini_{SanitizeFileNameWithoutExtension(primaryName)}_vs_{SanitizeFileNameWithoutExtension(referenceName)}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    [HttpPost("text")]
    public async Task<ActionResult<InternalScanReportDto>> ScanText(
        [FromBody] TextScanRequestDto request,
        CancellationToken ct = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { errorMessage = "Textul introdus nu poate fi gol." });
        }

        const double threshold = 0.25;

        var title = string.IsNullOrWhiteSpace(request.Title)
            ? "Text introdus manual"
            : request.Title.Trim();

        var result = await _service.ScanTextAsync(title, request.Text, threshold, ct);

        return Ok(result);
    }

    private static byte[] BuildPdf(
        InternalScanReportDto report,
        string title,
        string subtitle,
        string documentInfo,
        string analyzedDocumentName)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        const string PrimaryColor = "#2563EB";
        const string ExactColor = "#5B5BD6";
        const string ParaphraseColor = "#7CC6F8";
        const string NeutralColor = "#E8EEF7";
        const string TextColor = "#0F172A";
        const string MutedColor = "#64748B";
        const string BorderColor = "#DBE4F0";
        const string SoftBackground = "#F8FBFF";
        const string PageBackground = "#F4F7FB";

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

        string LevelLabel(int p)
            => p >= 70 ? "Nivel ridicat"
             : p >= 30 ? "Nivel mediu"
             : "Nivel scăzut";

        string LevelColor(int p)
            => p >= 70 ? ExactColor
             : p >= 30 ? PrimaryColor
             : "#0F766E";

        var groupedFragments = report.Fragments?
            .OrderByDescending(x => x.Type)
            .ThenByDescending(x => x.Score)
            .Take(30)
            .GroupBy(x => x.ReferenceFileName)
            .ToList() ?? new List<IGrouping<string, InternalScanFragmentDto>>();

        static IContainer Card(IContainer container)
        {
            return container
                .Background(Colors.White)
                .Border(1)
                .BorderColor("#DBE4F0")
                .Padding(14);
        }

        static IContainer SoftCard(IContainer container)
        {
            return container
                .Background("#F8FBFF")
                .Border(1)
                .BorderColor("#DBE4F0")
                .Padding(10);
        }

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.PageColor(PageBackground);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(TextColor));

                page.Header().Element(header =>
                {
                    header
                        .Background(Colors.White)
                        .Border(1)
                        .BorderColor(BorderColor)
                        .Padding(16)
                        .Column(col =>
                        {
                            col.Spacing(8);

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Column(left =>
                                {
                                    left.Item().Text("SimilarityChecker")
                                        .FontSize(9)
                                        .FontColor(PrimaryColor)
                                        .SemiBold();

                                    left.Item().Text(title)
                                        .FontSize(20)
                                        .FontColor(TextColor)
                                        .Bold();

                                    left.Item().Text(subtitle)
                                        .FontSize(11)
                                        .FontColor(MutedColor);

                                    left.Item().PaddingTop(4).Text(documentInfo)
                                        .FontSize(9)
                                        .FontColor(MutedColor);
                                });

                                row.ConstantItem(175).AlignRight().Column(right =>
                                {
                                    right.Item().AlignRight().Text("Raport PDF")
                                        .FontSize(10)
                                        .FontColor(PrimaryColor)
                                        .SemiBold();

                                    right.Item().AlignRight().Text($"Generat: {report.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC")
                                        .FontSize(8)
                                        .FontColor(MutedColor);
                                });
                            });
                        });
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Spacing(15);

                    col.Item().Text("1. Rezumatul analizei")
                        .FontSize(14)
                        .SemiBold()
                        .FontColor(TextColor);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem(1).Element(Card).Column(c =>
                        {
                            c.Spacing(7);

                            c.Item().Text("Scor final")
                                .FontSize(10)
                                .FontColor(MutedColor)
                                .SemiBold();

                            c.Item().Text($"{suspiciousTotal}%")
                                .FontSize(38)
                                .Bold()
                                .FontColor(LevelColor(suspiciousTotal));

                            c.Item().Text(LevelLabel(suspiciousTotal))
                                .FontSize(11)
                                .SemiBold()
                                .FontColor(LevelColor(suspiciousTotal));

                            c.Item().PaddingTop(4).Text("Procent estimat de conținut similar identificat în raport cu sursele interne.")
                                .FontSize(8)
                                .FontColor(MutedColor);
                        });

                        row.ConstantItem(12);

                        row.RelativeItem(2).Element(Card).Column(c =>
                        {
                            c.Spacing(10);

                            c.Item().Text("Distribuția conținutului")
                                .FontSize(11)
                                .SemiBold()
                                .FontColor(TextColor);

                            c.Item().Row(r =>
                            {
                                if (exactPct > 0)
                                    r.RelativeItem(exactPct).Height(16).Background(ExactColor);

                                if (paraPct > 0)
                                    r.RelativeItem(paraPct).Height(16).Background(ParaphraseColor);

                                if (cleanPct > 0)
                                    r.RelativeItem(cleanPct).Height(16).Background(NeutralColor);

                                if (exactPct == 0 && paraPct == 0 && cleanPct == 0)
                                    r.RelativeItem(1).Height(16).Background(NeutralColor);
                            });

                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Element(SoftCard).Column(x =>
                                {
                                    x.Item().Text("Potrivire exactă")
                                        .FontSize(8)
                                        .FontColor(MutedColor);

                                    x.Item().Text($"{exactPct}%")
                                        .FontSize(15)
                                        .SemiBold()
                                        .FontColor(ExactColor);
                                });

                                r.ConstantItem(8);

                                r.RelativeItem().Element(SoftCard).Column(x =>
                                {
                                    x.Item().Text("Parafrazare")
                                        .FontSize(8)
                                        .FontColor(MutedColor);

                                    x.Item().Text($"{paraPct}%")
                                        .FontSize(15)
                                        .SemiBold()
                                        .FontColor(ParaphraseColor);
                                });

                                r.ConstantItem(8);

                                r.RelativeItem().Element(SoftCard).Column(x =>
                                {
                                    x.Item().Text("Original estimat")
                                        .FontSize(8)
                                        .FontColor(MutedColor);

                                    x.Item().Text($"{cleanPct}%")
                                        .FontSize(15)
                                        .SemiBold()
                                        .FontColor(MutedColor);
                                });
                            });
                        });
                    });

                    col.Item().Text("2. Documente comparate")
                        .FontSize(14)
                        .SemiBold()
                        .FontColor(TextColor);

                    if (report.Hits.Count == 0)
                    {
                        col.Item().Element(Card).Text("Nu au fost găsite documente similare peste pragul stabilit.")
                            .FontColor(MutedColor);
                    }
                    else
                    {
                        col.Item().Element(Card).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(6);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(SoftBackground).Padding(8)
                                    .Text("Document de referință")
                                    .FontSize(9)
                                    .SemiBold()
                                    .FontColor(TextColor);

                                header.Cell().Background(SoftBackground).Padding(8).AlignRight()
                                    .Text("Scor comparativ")
                                    .FontSize(9)
                                    .SemiBold()
                                    .FontColor(TextColor);
                            });

                            foreach (var hit in report.Hits.OrderByDescending(x => x.SimilarityScore).Take(10))
                            {
                                var p = (int)Math.Round(hit.SimilarityScore * 100);

                                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(8)
                                    .Text(hit.ComparedFileName)
                                    .FontSize(9)
                                    .FontColor(TextColor);

                                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(8).AlignRight()
                                    .Text($"{p}%")
                                    .FontSize(10)
                                    .SemiBold()
                                    .FontColor(PrimaryColor);
                            }
                        });
                    }

                    col.Item().Text("3. Fragmente identificate")
                        .FontSize(14)
                        .SemiBold()
                        .FontColor(TextColor);

                    if (report.Fragments is null || report.Fragments.Count == 0)
                    {
                        col.Item().Element(Card).Text("Nu au fost identificate fragmente similare relevante.")
                            .FontColor(MutedColor);
                    }
                    else
                    {
                        foreach (var group in groupedFragments)
                        {
                            col.Item().PaddingTop(3).Text($"Sursă: {group.Key}")
                                .FontSize(10)
                                .SemiBold()
                                .FontColor(PrimaryColor);

                            foreach (var f in group)
                            {
                                var isExact = f.Type == FragmentTypeDto.Exact;
                                var label = isExact ? "Potrivire exactă" : "Potrivire parafrazată";
                                var pct = (int)Math.Round(f.Score * 100);
                                var fragmentColor = isExact ? ExactColor : ParaphraseColor;

                                var sourceSnippet = ShortenSnippet(f.SourceSnippet);
                                var referenceSnippet = ShortenSnippet(f.ReferenceSnippet);

                                col.Item()
                                    .Background(Colors.White)
                                    .Border(1)
                                    .BorderColor(BorderColor)
                                    .Padding(10)
                                    .Column(box =>
                                    {
                                        box.Spacing(7);

                                        box.Item().Row(r =>
                                        {
                                            r.ConstantItem(4).Height(14).Background(fragmentColor);

                                            r.ConstantItem(8);

                                            r.RelativeItem().Text($"{label} • {pct}%")
                                                .FontSize(10)
                                                .SemiBold()
                                                .FontColor(fragmentColor);
                                        });

                                        box.Item().LineHorizontal(1).LineColor(BorderColor);

                                        box.Item().Text("Fragment din documentul analizat:")
                                            .FontSize(8)
                                            .SemiBold()
                                            .FontColor(MutedColor);

                                        box.Item().Text(sourceSnippet)
                                            .FontSize(9)
                                            .FontColor(TextColor);

                                        box.Item().Text("Fragment din documentul de referință:")
                                            .FontSize(8)
                                            .SemiBold()
                                            .FontColor(MutedColor);

                                        box.Item().Text(referenceSnippet)
                                            .FontSize(9)
                                            .FontColor(TextColor);
                                    });
                            }
                        }

                        col.Item().Text("Notă: raportul afișează cel mult 30 de fragmente relevante. Fragmentele lungi sunt prezentate prescurtat, păstrând începutul și finalul textului.")
                            .FontSize(8)
                            .FontColor(MutedColor);
                    }
                });

                page.Footer().PaddingTop(12).Row(row =>
                {
                    row.RelativeItem()
                        .Text($"SimilarityChecker • Raport generat automat • {analyzedDocumentName}")
                        .FontSize(8)
                        .FontColor(MutedColor);

                    row.ConstantItem(120).AlignRight()
                        .Text(txt =>
                        {
                            txt.Span("Pagina ").FontSize(8).FontColor(MutedColor);
                            txt.CurrentPageNumber().FontSize(8).FontColor(MutedColor);
                            txt.Span(" / ").FontSize(8).FontColor(MutedColor);
                            txt.TotalPages().FontSize(8).FontColor(MutedColor);
                        });
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static string ShortenSnippet(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = NormalizeWhitespace(text);

        const int fullLimit = 430;
        const int startLength = 210;
        const int endLength = 170;

        if (normalized.Length <= fullLimit)
        {
            return normalized;
        }

        var start = normalized[..Math.Min(startLength, normalized.Length)].Trim();
        var end = normalized[^Math.Min(endLength, normalized.Length)..].Trim();

        return $"{start} ... {end}";
    }

    private static string NormalizeWhitespace(string text)
    {
        return string.Join(
            " ",
            text.Split(
                new[] { ' ', '\r', '\n', '\t' },
                StringSplitOptions.RemoveEmptyEntries));
    }

    private static string SanitizeFileNameWithoutExtension(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);

        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Document";
        }

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalidChar, '_');
        }

        name = name
            .Replace(" ", "_")
            .Replace("-", "_")
            .Replace(".", "_");

        while (name.Contains("__", StringComparison.Ordinal))
        {
            name = name.Replace("__", "_");
        }

        name = name.Trim('_');

        return string.IsNullOrWhiteSpace(name)
            ? "Document"
            : name;
    }
}