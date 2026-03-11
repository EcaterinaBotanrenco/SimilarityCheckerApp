using SimilarityChecker.Shared.Dtos;
using System.Threading;
using System.Threading.Tasks;

namespace SimilarityChecker.UI.Services
{
    public interface IProfileApiClient
    {
        Task<UserProfileDto> GetMyProfileAsync(CancellationToken ct = default);
    }
}