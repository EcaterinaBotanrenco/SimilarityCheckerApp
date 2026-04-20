using System.Linq;
using Microsoft.EntityFrameworkCore;
using SimilarityChecker.Api.Data;
using SimilarityChecker.Api.Data.Entities;
using SimilarityChecker.Api.Models;
using SimilarityChecker.Api.Services.Storage;
using SimilarityChecker.Shared.Dto;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SimilarityChecker.Api.Services.InternalScan;

public sealed class InternalScanService : IInternalScanService
{
    private readonly SimilarityCheckerDbContext _db;
    private readonly IDocumentStorageService _storage;

    public InternalScanService(SimilarityCheckerDbContext db, IDocumentStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<InternalScanStartResponse> StartAsync(Guid documentId, double threshold, CancellationToken ct)
    {
        var allDocuments = await _db.Documents
            .AsNoTracking()
            .ToListAsync(ct);

        var doc = allDocuments.FirstOrDefault(d => d.Id == documentId);
        if (doc is null)
            throw new InvalidOperationException("Documentul nu există în baza de date.");

        var textMap = await LoadTextsAsync(allDocuments, ct);
        var currentText = textMap.GetValueOrDefault(doc.Id, string.Empty);

        if (string.IsNullOrWhiteSpace(currentText))
            throw new InvalidOperationException("Documentul analizat nu conține text procesabil.");

        var others = allDocuments.Where(d => d.Id != documentId).ToList();

        var oldInternal = _db.InternalMatches.Where(m => m.DocumentId == documentId);
        _db.InternalMatches.RemoveRange(oldInternal);

        var hits = new List<InternalScanHitDto>();

        var corpusTexts = others
            .Select(x => textMap.GetValueOrDefault(x.Id, string.Empty))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        foreach (var other in others)
        {
            var otherText = textMap.GetValueOrDefault(other.Id, string.Empty);
            if (string.IsNullOrWhiteSpace(otherText))
                continue;

            double score;

            if (!string.IsNullOrWhiteSpace(doc.Sha256) && doc.Sha256 == other.Sha256)
            {
                score = 1.0;
            }
            else
            {
                score = InternalSimilarityEngine.ComputeSimilarity(currentText, otherText, corpusTexts);
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
                    AlgorithmVersion = "internal-v3-file-storage"
                });
            }
        }

        hits = hits.OrderByDescending(x => x.SimilarityScore).ToList();

        const int topK = 5;
        var topRefs = hits.Take(topK).Select(h => h.ComparedDocumentId).ToHashSet();

        var refDocs = others.Where(d => topRefs.Contains(d.Id)).ToList();

        var fragments = new List<InternalScanFragmentDto>();

        foreach (var refDoc in refDocs)
        {
            var refText = textMap.GetValueOrDefault(refDoc.Id, string.Empty);
            if (string.IsNullOrWhiteSpace(refText))
                continue;

            var exacts = InternalSimilarityEngine.FindExactFragments(
                currentText,
                refText,
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
                currentText,
                refText,
                minCombinedScore: 0.38,
                maxExactSimilarity: 0.90,
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

        var sourceTokenCount = CountTokens(currentText);

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
            ReportId = Guid.Empty,
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

    public async Task<InternalScanReportDto> CompareTwoDocumentsAsync(
        Guid primaryDocumentId,
        Guid referenceDocumentId,
        double threshold,
        CancellationToken ct = default)
    {
        if (primaryDocumentId == referenceDocumentId)
            throw new InvalidOperationException("Documentul analizat și documentul de referință nu pot fi identice.");

        var docs = await _db.Documents
            .AsNoTracking()
            .Where(x => x.Id == primaryDocumentId || x.Id == referenceDocumentId)
            .ToListAsync(ct);

        var primaryDocument = docs.FirstOrDefault(x => x.Id == primaryDocumentId);
        if (primaryDocument is null)
            throw new InvalidOperationException("Documentul analizat nu a fost găsit.");

        var referenceDocument = docs.FirstOrDefault(x => x.Id == referenceDocumentId);
        if (referenceDocument is null)
            throw new InvalidOperationException("Documentul de referință nu a fost găsit.");

        var primaryText = await LoadDocumentTextAsync(primaryDocument, ct);
        var referenceText = await LoadDocumentTextAsync(referenceDocument, ct);

        var corpusDocuments = await _db.Documents
            .AsNoTracking()
            .Where(d => d.Id != primaryDocumentId && d.Id != referenceDocumentId)
            .ToListAsync(ct);

        var corpusTexts = new List<string>();
        foreach (var item in corpusDocuments)
        {
            var text = await LoadDocumentTextAsync(item, ct);
            if (!string.IsNullOrWhiteSpace(text))
                corpusTexts.Add(text);
        }

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
            similarityScore = InternalSimilarityEngine.ComputeSimilarity(primaryText, referenceText, corpusTexts);
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
            minCombinedScore: 0.38,
            maxExactSimilarity: 0.90,
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

    private async Task<Dictionary<Guid, string>> LoadTextsAsync(IEnumerable<DocumentEntity> documents, CancellationToken ct)
    {
        var result = new Dictionary<Guid, string>();

        foreach (var doc in documents)
        {
            result[doc.Id] = await LoadDocumentTextAsync(doc, ct);
        }

        return result;
    }

    private async Task<string> LoadDocumentTextAsync(DocumentEntity doc, CancellationToken ct)
    {
        return await _storage.ReadExtractedTextAsync(doc.ExtractedTextPath, ct);
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
}