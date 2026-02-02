namespace SimilarityChecker.UI.Services.TextExtraction
{
    public sealed class TextExtractionService
    {
        private readonly IEnumerable<ITextExtractor> _extractors;

        public TextExtractionService(IEnumerable<ITextExtractor> extractors)
            => _extractors = extractors;

        public Task<string> ExtractAsync(byte[] bytes, string fileName, CancellationToken ct = default)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var extractor = _extractors.FirstOrDefault(x => x.CanHandle(ext));

            if (extractor is null)
                throw new NotSupportedException($"Tip de fișier nesuportat: {ext}");

            return extractor.ExtractTextAsync(bytes, fileName, ct);
        }
    }

}
