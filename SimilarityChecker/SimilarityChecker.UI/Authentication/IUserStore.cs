using Microsoft.PowerBI.Api.Models;
using System.Threading.Tasks;

namespace SimilarityChecker.UI.Authentication
{
    public interface IUserStore
    {
        Task<AppUser?> FindByEmailAsync(string email);
        Task<bool> EmailExistsAsync(string email);
        Task CreateAsync(AppUser user);
    }
}
