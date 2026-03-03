using Microsoft.AspNetCore.Mvc;
using QuestPDF.Helpers;
using SimilarityChecker.Api.Models;
using SimilarityChecker.Api.Services.InternalScan;
using SimilarityChecker.Shared.Dto;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SimilarityChecker.Shared.Dtos;
using System.Text;
using Document = System.Reflection.Metadata.Document;
using InternalScanReportDto = SimilarityChecker.Shared.Dto.InternalScanReportDto;
using FragmentTypeDto = SimilarityChecker.Shared.Dto.FragmentTypeDto;
using InternalScanFragmentDto = SimilarityChecker.Shared.Dtos.InternalScanFragmentDto;

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
        const double threshold = 0.25; // prag global fix
        var result = await _service.StartAsync(documentId, threshold, ct);
        return Ok(result);
    }

    [HttpGet("report/{documentId:guid}")]
    public async Task<ActionResult<InternalScanReportDto>> GetReport(Guid documentId, CancellationToken ct)
    {
        var report = await _service.GetReportAsync(documentId, ct);
        if (report is null) return NotFound("Nu există raport pentru acest document.");
        return Ok(report);
    }

    [HttpGet("report/{documentId:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid documentId, CancellationToken ct)
    {
        var report = await _service.GetReportAsync(documentId, ct);
        if (report is null) return NotFound("Nu există raport pentru acest document.");

        var pdfBytes = BuildPdf(report);

        var fileName = $"raport-scanare-interna-{documentId}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    private static byte[] BuildPdf(InternalScanReportDto report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        int overall = report.Hits.Count == 0 ? 0
            : (int)Math.Round(report.Hits.Max(x => x.SimilarityScore) * 100);

        int avg = report.Hits.Count == 0 ? 0
            : (int)Math.Round(report.Hits.Average(x => x.SimilarityScore) * 100);

        // Procente tri-color (dacă nu sunt setate, derivăm un minim din overall)
        int exactPct = report.ExactPercent;
        int paraPct = report.ParaphrasePercent;
        int cleanPct = report.CleanPercent;

        if (exactPct == 0 && paraPct == 0 && cleanPct == 0)
        {
            // fallback sigur
            exactPct = overall;
            paraPct = 0;
            cleanPct = Math.Max(0, 100 - exactPct);
        }

        string LevelLabel(int p) => p >= 70 ? "Nivel ridicat" : p >= 30 ? "Nivel mediu" : "Nivel scăzut";

        var doc = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(11));

                // ===== HEADER =====
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Raport de similitudine")
                            .FontSize(18).SemiBold();

                        col.Item().Text("Scanare internă")
                            .FontSize(12).FontColor(Colors.Grey.Darken2);
                    });

                    row.ConstantItem(220).AlignRight().Column(col =>
                    {
                        col.Item().Text($"Generat: {report.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss} UTC")
                            .FontSize(10).FontColor(Colors.Grey.Darken2);

                        col.Item().Text($"DocumentId: {report.DocumentId}")
                            .FontSize(8).FontColor(Colors.Grey.Darken2);
                    });
                });

                // ===== CONTENT =====
                page.Content().Column(col =>
                {
                    col.Spacing(14);

                    // --- Secțiune: Rezumat ---
                    col.Item().Text("1. Rezumat")
                        .FontSize(13).SemiBold();

                    col.Item().Row(row =>
                    {
                        // Card stânga: scoruri
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(c =>
                        {
                            c.Spacing(6);

                            c.Item().Text("Indicatori principali").SemiBold();

                            c.Item().Text($"Nivel maxim (overall): {overall}%  •  {LevelLabel(overall)}");
                            c.Item().Text($"Media (top rezultate): {avg}%");
                            c.Item().Text($"Documente comparate: {report.Hits.Count}");

                            c.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            c.Item().Text("Distribuție conținut").SemiBold();
                            c.Item().Text($"Copiat exact: {exactPct}%");
                            c.Item().Text($"Parafrazat: {paraPct}%");
                            c.Item().Text($"Curat: {cleanPct}%");
                        });

                        // Card dreapta: bară tri-color (vizual)
                        row.ConstantItem(220).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(c =>
                        {
                            c.Spacing(10);
                            c.Item().Text("Vizual").SemiBold();

                            // bară tri-color
                            c.Item().Row(r =>
                            {
                                if (exactPct > 0) r.RelativeItem(exactPct).Height(14).Background(Colors.Red.Medium);
                                if (paraPct > 0) r.RelativeItem(paraPct).Height(14).Background(Colors.Orange.Medium);
                                if (cleanPct > 0) r.RelativeItem(cleanPct).Height(14).Background(Colors.Green.Medium);

                                // dacă toate sunt 0 (caz rar), punem o bară neutră
                                if (exactPct == 0 && paraPct == 0 && cleanPct == 0)
                                    r.RelativeItem(1).Height(14).Background(Colors.Grey.Lighten2);
                            });

                            c.Item().Text("Legendă:")
                                .FontSize(10).FontColor(Colors.Grey.Darken2);

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
                                r.RelativeItem().Text("Curat").FontSize(10);
                            });
                        });
                    });

                    // --- Secțiune: Rezultate Top ---
                    col.Item().Text("2. Rezultate (Top documente similare)")
                        .FontSize(13).SemiBold();

                    if (report.Hits.Count == 0)
                    {
                        col.Item().Text("Nu au fost găsite documente similare peste prag.")
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
                                    .Text("Document comparat").SemiBold();

                                header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight()
                                    .Text("Scor").SemiBold();
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

                    // --- Secțiune: Fragmente (doar în raport) ---
                    col.Item().Text("3. Fragmente identificate")
                        .FontSize(13).SemiBold();

                    if (report.Fragments is null || report.Fragments.Count == 0)
                    {
                        col.Item().Text("Nu au fost identificate fragmente copiate în document.")
                            .FontColor(Colors.Grey.Darken2);
                    }
                    else
                    {
                        // limităm ca PDF-ul să nu devină uriaș (poți mări)
                        foreach (var f in report.Fragments
                                     .OrderByDescending(x => x.Type) // Exact înaintea Paraphrase
                                     .ThenByDescending(x => x.Score)
                                     .Take(30))
                        {
                            var label = f.Type == FragmentTypeDto.Exact ? "Copiat exact" : "Parafrazat";
                            var pct = (int)Math.Round(f.Score * 100);

                            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(box =>
                            {
                                box.Spacing(6);

                                // header fragment
                                box.Item().Row(r =>
                                {
                                    // badge tip
                                    r.RelativeItem().Text($"{label} • {pct}%")
                                        .SemiBold()
                                        .FontColor(f.Type == FragmentTypeDto.Exact ? Colors.Red.Darken2 : Colors.Orange.Darken2);

                                    r.ConstantItem(240).AlignRight()
                                        .Text($"Sursă: {f.ReferenceFileName}")
                                        .FontSize(10).FontColor(Colors.Grey.Darken2);
                                });

                                box.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                                // snippet-uri
                                box.Item().Text("În documentul încărcat:")
                                    .FontSize(10).FontColor(Colors.Grey.Darken2);

                                box.Item().Text(f.SourceSnippet)
                                    .FontSize(10);

                                box.Item().Text("În documentul sursă:")
                                    .FontSize(10).FontColor(Colors.Grey.Darken2);

                                box.Item().Text(f.ReferenceSnippet)
                                    .FontSize(10);
                            });
                        }

                        col.Item().Text("*Se afișează maxim 30 fragmente (cele mai relevante).")
                            .FontSize(9).FontColor(Colors.Grey.Darken2);
                    }
                });

                // ===== FOOTER =====
                page.Footer().Row(row =>
                {
                    row.RelativeItem()
                        .Text("SimilarityChecker • Raport generat automat")
                        .FontSize(9).FontColor(Colors.Grey.Darken2);

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
