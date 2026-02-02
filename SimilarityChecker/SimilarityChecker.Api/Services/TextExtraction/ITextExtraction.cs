using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

//Contract
namespace SimilarityChecker.UI.Services.TextExtraction
{
    public interface ITextExtractor
    {
        bool CanHandle(string extension);
        Task<string> ExtractTextAsync(byte[] fileBytes, string fileName, CancellationToken ct = default);
    }

}
