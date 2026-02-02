using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimilarityChecker.UI.Services
{
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

    public interface IPlagiarismService
    {
        Task<PlagiarismCheckResponse> CheckAsync(PlagiarismCheckRequest request);

        // pentru PDF: obții din server datele pentru runId
        Task<PlagiarismResultDto?> GetResultAsync(Guid runId);
    }
}
