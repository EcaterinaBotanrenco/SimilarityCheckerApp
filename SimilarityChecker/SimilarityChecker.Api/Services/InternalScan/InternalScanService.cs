using Microsoft.EntityFrameworkCore;
using SimilarityChecker.Api.Data;
using SimilarityChecker.Api.Data.Entities;
using SimilarityChecker.Api.Models;
using SimilarityChecker.Shared.Dto;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SimilarityChecker.Api.Services.InternalScan;

public interface IInternalScanService
{
    Task<InternalScanStartResponse> StartAsync(Guid documentId, double threshold, CancellationToken ct);
    Task<InternalScanReportDto?> GetReportAsync(Guid documentId, CancellationToken ct);
}

public sealed class InternalScanService : IInternalScanService
{
    private readonly SimilarityCheckerDbContext _db;

    public InternalScanService(SimilarityCheckerDbContext db) => _db = db;

    public async Task<InternalScanStartResponse> StartAsync(Guid documentId, double threshold, CancellationToken ct)
    {
        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (doc is null)
            throw new InvalidOperationException("Documentul nu există în baza de date.");

        var others = await _db.Documents
            .Where(d => d.Id != documentId)
            .Select(d => new { d.Id, d.FileName, d.ExtractedText, d.Sha256 })
            .ToListAsync(ct);

        // recomandat: curăță match-urile vechi pentru acest document
        var oldInternal = _db.InternalMatches.Where(m => m.DocumentId == documentId);
        _db.InternalMatches.RemoveRange(oldInternal);

        var hits = new List<InternalScanHitDto>();

        foreach (var other in others)
        {
            double score;

            // 1) duplicat exact (hash)
            if (!string.IsNullOrWhiteSpace(doc.Sha256) && doc.Sha256 == other.Sha256)
            {
                score = 1.0;
            }
            else
            {
                var source = doc.ExtractedText ?? "";
                var target = other.ExtractedText ?? "";

                if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
                    continue;

                score = InternalSimilarityEngine.ComputeSimilarity(source, target);
            }

            if (score >= threshold)
            {
                hits.Add(new InternalScanHitDto
                {
                    ComparedDocumentId = other.Id,
                    ComparedFileName = other.FileName,
                    SimilarityScore = score
                });

                _db.InternalMatches.Add(new InternalMatchEntity
                {
                    DocumentId = doc.Id,
                    ComparedDocumentId = other.Id,
                    Score = score,
                    AlgorithmVersion = "internal-v2"
                });
            }
        }

        hits = hits.OrderByDescending(x => x.SimilarityScore).ToList();

        // ===== NOU: Fragmente “Exact” pentru top K rezultate =====
        const int topK = 5;
        var topRefs = hits.Take(topK).Select(h => h.ComparedDocumentId).ToHashSet();

        var refDocs = await _db.Documents
            .Where(d => topRefs.Contains(d.Id))
            .Select(d => new { d.Id, d.FileName, d.ExtractedText })
            .ToListAsync(ct);

        var fragments = new List<InternalScanFragmentDto>();

        foreach (var refDoc in refDocs)
        {
            var exacts = InternalSimilarityEngine.FindExactFragments(
                doc.ExtractedText,
                refDoc.ExtractedText,
                minConsecutiveShingles: 3);

            foreach (var ex in exacts)
            {
                fragments.Add(new InternalScanFragmentDto
                {
                    Type = FragmentTypeDto.Exact,
                    Score = ex.Score,
                    SourceTokenStart = ex.SourceTokenStart,
                    SourceTokenEnd = ex.SourceTokenEnd,
                    RefTokenStart = ex.RefTokenStart,
                    RefTokenEnd = ex.RefTokenEnd,
                    ReferenceDocumentId = refDoc.Id,
                    ReferenceFileName = refDoc.FileName,
                    SourceSnippet = ex.SourceSnippet,
                    ReferenceSnippet = ex.ReferenceSnippet
                });
            }
        }

        // ===== NOU: Procente tri-color =====
        var sourceTokenCount = CountTokens(doc.ExtractedText);
        var exactCovered = EstimateCoveredTokens(fragments.Where(f => f.Type == FragmentTypeDto.Exact));

        int exactPercent = sourceTokenCount == 0 ? 0 : (int)Math.Round(100.0 * exactCovered / sourceTokenCount);
        int paraphrasePercent = 0; // va fi completat când adăugăm TF-IDF + Cosine
        int cleanPercent = Math.Max(0, 100 - exactPercent - paraphrasePercent);

        var reportDto = new InternalScanReportDto
        {
            ReportId = Guid.Empty, // completăm după SaveChanges
            DocumentId = doc.Id,
            GeneratedAtUtc = DateTime.UtcNow,
            Hits = hits,
            Fragments = fragments,
            ExactPercent = exactPercent,
            ParaphrasePercent = paraphrasePercent,
            CleanPercent = cleanPercent
        };

        var report = new ReportEntity
        {
            DocumentId = doc.Id,
            UserId = doc.UserId,
            GeneratedAtUtc = DateTime.UtcNow,
            Status = "Completed",
            ReportJson = JsonSerializer.Serialize(reportDto)
        };

        _db.Reports.Add(report);
        await _db.SaveChangesAsync(ct);

        // update ReportId în JSON
        reportDto.ReportId = report.Id;
        report.ReportJson = JsonSerializer.Serialize(reportDto);
        await _db.SaveChangesAsync(ct);

        return new InternalScanStartResponse
        {
            ReportId = report.Id,
            DocumentId = doc.Id,
            ComparedDocuments = others.Count
        };
    }

    public async Task<InternalScanReportDto?> GetReportAsync(Guid documentId, CancellationToken ct)
    {
        var report = await _db.Reports
            .Where(r => r.DocumentId == documentId)
            .OrderByDescending(r => r.GeneratedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (report is null) return null;

        var dto = JsonSerializer.Deserialize<InternalScanReportDto>(report.ReportJson);
        if (dto is null) return null;

        dto.ReportId = report.Id;
        return dto;
    }

    private static int CountTokens(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        var cleaned = Regex.Replace(text.ToLowerInvariant(), @"[^\p{L}\p{N}\s]+", " ");
        return Regex.Matches(cleaned, @"\S+").Count;
    }

    private static int EstimateCoveredTokens(IEnumerable<InternalScanFragmentDto> frags)
    {
        var intervals = frags
            .Select(f => (s: f.SourceTokenStart, e: f.SourceTokenEnd))
            .Where(x => x.e > x.s)
            .OrderBy(x => x.s)
            .ToList();

        if (intervals.Count == 0) return 0;

        int covered = 0;
        int curS = intervals[0].s, curE = intervals[0].e;

        for (int i = 1; i < intervals.Count; i++)
        {
            var (s, e) = intervals[i];
            if (s <= curE) curE = Math.Max(curE, e);
            else
            {
                covered += (curE - curS);
                curS = s;
                curE = e;
            }
        }

        covered += (curE - curS);
        return covered;
    }
}