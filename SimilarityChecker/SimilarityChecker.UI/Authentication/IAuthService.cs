using System.Threading.Tasks;

namespace SimilarityChecker.UI.Authentication
{
    public interface IAuthService
    {
        Task<AuthResult> SignInAsync(string email, string password, bool rememberMe);
        Task<AuthResult> SignUpAsync(string firstName, string lastName, string email, string password, string role);
        Task SignOutAsync();
        Task RequestPasswordResetAsync(string email);
        Task ResetPasswordAsync(string email, string token, string newPassword);
    }
}