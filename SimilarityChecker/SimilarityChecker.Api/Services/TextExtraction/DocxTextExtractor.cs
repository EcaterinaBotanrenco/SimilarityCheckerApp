using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using SimilarityChecker.UI.Services.TextExtraction;
using System.Text;

namespace SimilarityChecker.Api.Services.TextExtraction
{
    public sealed class DocxTextExtractor : ITextExtractor
    {
        public bool CanHandle(string extension) => extension == ".docx";

        public Task<string> ExtractTextAsync(byte[] fileBytes, string fileName, CancellationToken ct = default)
        {
            using var ms = new MemoryStream(fileBytes);
            using var doc = WordprocessingDocument.Open(ms, false);

            var body = doc.MainDocumentPart?.Document?.Body;
            if (body is null) return Task.FromResult(string.Empty);

            var sb = new StringBuilder();
            foreach (var text in body.Descendants<Text>())
            {
                sb.Append(text.Text);
                sb.Append(' ');
            }

            return Task.FromResult(sb.ToString());
        }
    }
}
