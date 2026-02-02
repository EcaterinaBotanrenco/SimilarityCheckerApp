using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimilarityChecker.UI.Services
{
    public sealed class DemoPlagiarismService : IPlagiarismService
    {
        private readonly Dictionary<Guid, PlagiarismResultDto> _store = new();

        public Task<PlagiarismCheckResponse> CheckAsync(PlagiarismCheckRequest request)
        {
            // TODO: aici vei pune extragerea textului + algoritm similaritate
            // acum simulăm rezultate
            var rnd = new Random();

            var result = new PlagiarismResultDto
            {
                Threshold = 25,
                OverallSimilarity = request.ReferenceContent is null ? rnd.Next(5, 35) : rnd.Next(15, 70),
                Matches = new List<PlagiarismMatchDto>
            {
                new() { SourceName = request.ReferenceFileName ?? "Bază internă (demo)", Similarity = rnd.Next(10, 60), Note = "Potrivire parțială." },
                new() { SourceName = "Sursă externă (demo)", Similarity = rnd.Next(5, 40), Note = "Fragmente similare." }
            }
            };

            var runId = Guid.NewGuid();
            _store[runId] = result;

            return Task.FromResult(new PlagiarismCheckResponse
            {
                RunId = runId,
                Result = result
            });
        }

        public Task<PlagiarismResultDto?> GetResultAsync(Guid runId)
        {
            _store.TryGetValue(runId, out var res);
            return Task.FromResult(res);
        }
    }
}
