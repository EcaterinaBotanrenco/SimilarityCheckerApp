using SimilarityChecker.Api.Services.TextProcessing;
using SimilarityChecker.UI.Services;
using SimilarityChecker.UI.Services.TextExtraction;

namespace SimilarityChecker.Api.Services.Plagiarism
{
    public sealed class PlagiarismService : IPlagiarismService
    {
        private readonly TextExtractionService _extract;
        private readonly Dictionary<Guid, PlagiarismResultDto> _store = new();

        public PlagiarismService(TextExtractionService extract)
            => _extract = extract;

        public async Task<PlagiarismCheckResponse> CheckAsync(PlagiarismCheckRequest request)
        {
            // 1) extract text
            var mainTextRaw = await _extract.ExtractAsync(request.MainContent, request.MainFileName);
            var refTextRaw = request.ReferenceContent is null
                ? null
                : await _extract.ExtractAsync(request.ReferenceContent, request.ReferenceFileName ?? "reference");

            // 2) normalize + tokenize
            var mainNorm = TextNormalizer.Normalize(mainTextRaw);
            var mainTokens = TextNormalizer.TokenizeWords(mainNorm);

            List<string>? refTokens = null;
            if (!string.IsNullOrWhiteSpace(refTextRaw))
            {
                var refNorm = TextNormalizer.Normalize(refTextRaw);
                refTokens = TextNormalizer.TokenizeWords(refNorm);
            }

            // 3) similarity (word shingles)
            var shingleSize = 5;

            var mainSh = ShinglingSimilarity.BuildWordShingles(mainTokens, shingleSize);
            var refSh = (refTokens is null)
                ? new HashSet<string>()
                : ShinglingSimilarity.BuildWordShingles(refTokens, shingleSize);

            var overall = (refTokens is null) ? 0 : ShinglingSimilarity.JaccardPercent(mainSh, refSh);

            // 4) build matches list (minim util acum)
            var matches = new List<PlagiarismMatchDto>();

            if (refTokens is not null)
            {
                matches.Add(new PlagiarismMatchDto
                {
                    SourceName = request.ReferenceFileName ?? "Document de referință",
                    Similarity = overall,
                    Note = $"Comparare directă (shingles {shingleSize})."
                });
            }
            else
            {
                matches.Add(new PlagiarismMatchDto
                {
                    SourceName = "Nu a fost încărcat document de referință",
                    Similarity = 0,
                    Note = "Încarcă un document de referință pentru comparare."
                });
            }

            // 5) finalize
            var result = new PlagiarismResultDto
            {
                Threshold = 25,
                OverallSimilarity = overall,
                Matches = matches
            };

            var runId = Guid.NewGuid();
            _store[runId] = result;

            return new PlagiarismCheckResponse { RunId = runId, Result = result };
        }

        public Task<PlagiarismResultDto?> GetResultAsync(Guid runId)
        {
            _store.TryGetValue(runId, out var res);
            return Task.FromResult(res);
        }
    }

}
