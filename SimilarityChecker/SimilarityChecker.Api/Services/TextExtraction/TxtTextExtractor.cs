using SimilarityChecker.UI.Services.TextExtraction;
using System.Text;

namespace SimilarityChecker.Api.Services.TextExtraction
{

    public sealed class TxtTextExtractor : ITextExtractor
    {
        public bool CanHandle(string extension) => extension == ".txt";

        public Task<string> ExtractTextAsync(byte[] fileBytes, string fileName, CancellationToken ct = default)
        {
            // Simplu: UTF-8. Dacă ai fișiere cp1251 (rusă), îți recomand Ude.NetStandard.
            var text = Encoding.UTF8.GetString(fileBytes);
            return Task.FromResult(text);
        }
    }
}
