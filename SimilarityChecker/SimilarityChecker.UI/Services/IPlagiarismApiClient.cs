using Microsoft.AspNetCore.Components.Forms;
using SimilarityChecker.Shared.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace SimilarityChecker.UI.Services;

public interface IPlagiarismApiClient
{
    Task<PlagiarismCheckResponse> CheckAsync(
        IBrowserFile mainFile,
        IBrowserFile? referenceFile,
        CancellationToken ct = default);
}
