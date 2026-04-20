using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimilarityChecker.Shared.Dto
{
    public sealed class DocumentUploadResponseDto
    {
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int WordCount { get; set; }
        public string Sha256 { get; set; } = string.Empty;
    }
    public sealed class MultipleDocumentUploadResponse
    {
        public int TotalFiles { get; set; }
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<SingleDocumentImportResult> Results { get; set; } = new();
    }

    public sealed class SingleDocumentImportResult
    {
        public string FileName { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public bool IsDuplicate { get; set; }
        public Guid? DocumentId { get; set; }
        public int? WordCount { get; set; }
        public string? Sha256 { get; set; }
        public string Message { get; set; } = string.Empty;
    }
    public sealed class InternalScanStartResponseDto
    {
        public Guid ReportId { get; set; }
        public Guid DocumentId { get; set; }
        public int ComparedDocuments { get; set; }
    }
    public sealed class PlagiarismCheckRequest
    {
        public string MainFileName { get; set; } = "";
        public byte[] MainContent { get; set; } = Array.Empty<byte>();

        public string? ReferenceFileName { get; set; }
        public byte[]? ReferenceContent { get; set; }
    }

    public sealed class PlagiarismCheckResponse
    {
        public Guid RunId { get; set; }
        public PlagiarismResultDto Result { get; set; } = new();
    }

    public sealed class PlagiarismResultDto
    {
        // 0..100
        public int OverallSimilarity { get; set; }
        public int Threshold { get; set; } = 25;

        public List<PlagiarismMatchDto> Matches { get; set; } = new();
    }

    public sealed class PlagiarismMatchDto
    {
        public string SourceName { get; set; } = "";
        public int Similarity { get; set; } // 0..100
        public string Note { get; set; } = "";
    }
    public sealed class InternalScanReportDto
    {
        public Guid ReportId { get; set; }
        public Guid DocumentId { get; set; }
        public DateTime GeneratedAtUtc { get; set; }

        // scor “document-level” (îl păstrăm)
        public List<InternalScanHitDto> Hits { get; set; } = new();

        // nou: fragmente
        public List<InternalScanFragmentDto> Fragments { get; set; } = new();

        // nou: procente pentru grafic 3 culori
        public int ExactPercent { get; set; }
        public int ParaphrasePercent { get; set; }
        public int CleanPercent { get; set; }
    }
    public enum FragmentTypeDto
    {
        Exact = 1,
        Paraphrase = 2
    }

    public sealed class InternalScanFragmentDto
    {
        public FragmentTypeDto Type { get; set; }
        public double Score { get; set; } // 0..1

        // fragment în documentul scanat (token indices)
        public int SourceTokenStart { get; set; }
        public int SourceTokenEnd { get; set; }

        // fragment în documentul de referință (token indices)
        public int RefTokenStart { get; set; }
        public int RefTokenEnd { get; set; }

        public Guid ReferenceDocumentId { get; set; }
        public string ReferenceFileName { get; set; } = string.Empty;

        // snippets gata de afișat (pentru raport/UI)
        public string SourceSnippet { get; set; } = string.Empty;
        public string ReferenceSnippet { get; set; } = string.Empty;
    }
    public sealed class InternalScanHitDto
    {
        public Guid ComparedDocumentId { get; set; }
        public string ComparedFileName { get; set; } = string.Empty;
        public double SimilarityScore { get; set; } // 0..1
    }
}
