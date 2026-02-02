using SimilarityChecker.UI.Services.TextExtraction;
using UglyToad.PdfPig;

namespace SimilarityChecker.Api.Services.TextExtraction
{
    public sealed class PdfTextExtractor : ITextExtractor
    {
        public bool CanHandle(string extension) => extension == ".pdf";

        public Task<string> ExtractTextAsync(byte[] fileBytes, string fileName, CancellationToken ct = default)
        {
            using var ms = new MemoryStream(fileBytes);
            using var pdf = PdfDocument.Open(ms);

            var parts = new List<string>(capacity: pdf.NumberOfPages);
            foreach (var page in pdf.GetPages())
                parts.Add(page.Text);

            return Task.FromResult(string.Join("\n", parts));
        }
    }
}
