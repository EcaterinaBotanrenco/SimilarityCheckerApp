using System.Linq;
using Microsoft.EntityFrameworkCore;
using SimilarityChecker.Api.Data;
using SimilarityChecker.Api.Data.Entities;
using SimilarityChecker.Api.Models;
using SimilarityChecker.Shared.Dto;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SimilarityChecker.Api.Services.InternalScan;

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

            var paraphrases = InternalSimilarityEngine.FindParaphraseFragments(
            doc.ExtractedText,
            refDoc.ExtractedText,
            minCombinedScore: 0.32,
            maxExactSimilarity: 0.85,
            minSharedContentWords: 2);

            foreach (var para in paraphrases)
            {
                bool overlapsExact = fragments.Any(f =>
                    f.Type == FragmentTypeDto.Exact &&
                    f.ReferenceDocumentId == refDoc.Id &&
                    RangesOverlap(f.SourceTokenStart, f.SourceTokenEnd, para.SourceTokenStart, para.SourceTokenEnd));

                if (overlapsExact)
                    continue;

                fragments.Add(new InternalScanFragmentDto
                {
                    Type = FragmentTypeDto.Paraphrase,
                    Score = para.Score,
                    SourceTokenStart = para.SourceTokenStart,
                    SourceTokenEnd = para.SourceTokenEnd,
                    RefTokenStart = para.RefTokenStart,
                    RefTokenEnd = para.RefTokenEnd,
                    ReferenceDocumentId = refDoc.Id,
                    ReferenceFileName = refDoc.FileName,
                    SourceSnippet = para.SourceSnippet,
                    ReferenceSnippet = para.ReferenceSnippet
                });
            }
        }

        // ===== NOU: Procente tri-color =====
        var sourceTokenCount = CountTokens(doc.ExtractedText);

        var exactCovered = EstimateCoveredTokens(
            fragments.Where(f => f.Type == FragmentTypeDto.Exact));

        var paraphraseCovered = EstimateCoveredTokens(
            fragments.Where(f => f.Type == FragmentTypeDto.Paraphrase));

        int exactPercent = 0;
        if (sourceTokenCount > 0 && exactCovered > 0)
        {
            exactPercent = (int)Math.Round(100.0 * exactCovered / sourceTokenCount);
            if (exactPercent == 0)
                exactPercent = 1;
        }

        int paraphrasePercent = 0;
        if (sourceTokenCount > 0 && paraphraseCovered > 0)
        {
            paraphrasePercent = (int)Math.Round(100.0 * paraphraseCovered / sourceTokenCount);
            if (paraphrasePercent == 0)
                paraphrasePercent = 1;
        }

        if (exactPercent + paraphrasePercent > 100)
        {
            paraphrasePercent = Math.Max(0, 100 - exactPercent);
        }

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
    private static bool RangesOverlap(int aStart, int aEnd, int bStart, int bEnd)
    {
        return aStart < bEnd && bStart < aEnd;
    }

    public async Task<InternalScanReportDto> CompareTwoDocumentsAsync(
    Guid primaryDocumentId,
    Guid referenceDocumentId,
    double threshold,
    CancellationToken ct = default)
    {
        if (primaryDocumentId == referenceDocumentId)
            throw new InvalidOperationException("Documentul analizat și documentul de referință nu pot fi identice.");

        var primaryDocument = await _db.Documents
            .FirstOrDefaultAsync(x => x.Id == primaryDocumentId, ct);

        if (primaryDocument is null)
            throw new InvalidOperationException("Documentul analizat nu a fost găsit.");

        var referenceDocument = await _db.Documents
            .FirstOrDefaultAsync(x => x.Id == referenceDocumentId, ct);

        if (referenceDocument is null)
            throw new InvalidOperationException("Documentul de referință nu a fost găsit.");

        var primaryText = primaryDocument.ExtractedText ?? "";
        var referenceText = referenceDocument.ExtractedText ?? "";

        if (string.IsNullOrWhiteSpace(primaryText))
            throw new InvalidOperationException("Documentul analizat nu conține text procesabil.");

        if (string.IsNullOrWhiteSpace(referenceText))
            throw new InvalidOperationException("Documentul de referință nu conține text procesabil.");

        double similarityScore;

        if (!string.IsNullOrWhiteSpace(primaryDocument.Sha256) &&
            primaryDocument.Sha256 == referenceDocument.Sha256)
        {
            similarityScore = 1.0;
        }
        else
        {
            similarityScore = InternalSimilarityEngine.ComputeSimilarity(primaryText, referenceText);
        }

        var fragments = new List<InternalScanFragmentDto>();

        var exacts = InternalSimilarityEngine.FindExactFragments(
            primaryText,
            referenceText,
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
                ReferenceDocumentId = referenceDocument.Id,
                ReferenceFileName = referenceDocument.FileName,
                SourceSnippet = ex.SourceSnippet,
                ReferenceSnippet = ex.ReferenceSnippet
            });
        }

        var paraphrases = InternalSimilarityEngine.FindParaphraseFragments(
        primaryText,
        referenceText,
        minCombinedScore: 0.32,
        maxExactSimilarity: 0.85,
        minSharedContentWords: 2);

        foreach (var para in paraphrases)
        {
            bool overlapsExact = fragments.Any(f =>
                f.Type == FragmentTypeDto.Exact &&
                RangesOverlap(f.SourceTokenStart, f.SourceTokenEnd, para.SourceTokenStart, para.SourceTokenEnd));

            if (overlapsExact)
                continue;

            fragments.Add(new InternalScanFragmentDto
            {
                Type = FragmentTypeDto.Paraphrase,
                Score = para.Score,
                SourceTokenStart = para.SourceTokenStart,
                SourceTokenEnd = para.SourceTokenEnd,
                RefTokenStart = para.RefTokenStart,
                RefTokenEnd = para.RefTokenEnd,
                ReferenceDocumentId = referenceDocument.Id,
                ReferenceFileName = referenceDocument.FileName,
                SourceSnippet = para.SourceSnippet,
                ReferenceSnippet = para.ReferenceSnippet
            });
        }

        var sourceTokenCount = CountTokens(primaryText);

        var exactCovered = EstimateCoveredTokens(
            fragments.Where(f => f.Type == FragmentTypeDto.Exact));

        var paraphraseCovered = EstimateCoveredTokens(
            fragments.Where(f => f.Type == FragmentTypeDto.Paraphrase));

        int exactPercent = 0;
        if (sourceTokenCount > 0 && exactCovered > 0)
        {
            exactPercent = (int)Math.Round(100.0 * exactCovered / sourceTokenCount);
            if (exactPercent == 0)
                exactPercent = 1;
        }

        int paraphrasePercent = 0;
        if (sourceTokenCount > 0 && paraphraseCovered > 0)
        {
            paraphrasePercent = (int)Math.Round(100.0 * paraphraseCovered / sourceTokenCount);
            if (paraphrasePercent == 0)
                paraphrasePercent = 1;
        }

        if (exactPercent + paraphrasePercent > 100)
        {
            paraphrasePercent = Math.Max(0, 100 - exactPercent);
        }

        int cleanPercent = Math.Max(0, 100 - exactPercent - paraphrasePercent);

        var report = new InternalScanReportDto
        {
            ReportId = Guid.Empty,
            DocumentId = primaryDocumentId,
            GeneratedAtUtc = DateTime.UtcNow,
            ExactPercent = exactPercent,
            ParaphrasePercent = paraphrasePercent,
            CleanPercent = cleanPercent,
            Hits = similarityScore >= threshold
                ? new List<InternalScanHitDto>
                {
                new InternalScanHitDto
                {
                    ComparedDocumentId = referenceDocument.Id,
                    ComparedFileName = referenceDocument.FileName,
                    SimilarityScore = similarityScore
                }
                }
                : new List<InternalScanHitDto>(),
            Fragments = fragments
        };

        return report;
    }
}