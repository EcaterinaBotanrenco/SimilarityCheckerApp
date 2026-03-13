using SimilarityChecker.Shared.Dtos;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SimilarityChecker.UI.Authentication
{
    public sealed class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthSessionStore _sessionStore;
        private readonly CustomAuthStateProvider _authState;

        public AuthService(
            HttpClient httpClient,
            AuthSessionStore sessionStore,
            CustomAuthStateProvider authState)
        {
            _httpClient = httpClient;
            _sessionStore = sessionStore;
            _authState = authState;
        }

        public async Task<AuthResult> SignInAsync(string email, string password, bool rememberMe)
        {
            var request = new LoginRequestDto
            {
                Email = email,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

            if (result is null)
                return AuthResult.Fail("Răspuns invalid de la server.");

            if (!response.IsSuccessStatusCode || !result.Success || result.User is null || string.IsNullOrWhiteSpace(result.Token))
                return AuthResult.Fail(result.ErrorMessage ?? "Autentificare eșuată.");

            _sessionStore.SetSession(new AuthSessionModel
            {
                Token = result.Token,
                UserId = result.User.Id,
                Email = result.User.Email,
                DisplayName = result.User.DisplayName,
                Roles = result.User.Roles
            });

            _authState.NotifyUserAuthentication();

            return AuthResult.Ok();
        }

        public async Task<AuthResult> SignUpAsync(string firstName, string lastName, string email, string password, string role)
        {
            var request = new RegisterRequestDto
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Password = password,
                Role = role
            };

            var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

            if (result is null)
                return AuthResult.Fail("Răspuns invalid de la server.");

            if (!response.IsSuccessStatusCode || !result.Success || result.User is null || string.IsNullOrWhiteSpace(result.Token))
                return AuthResult.Fail(result.ErrorMessage ?? "Înregistrare eșuată.");

            _sessionStore.SetSession(new AuthSessionModel
            {
                Token = result.Token,
                UserId = result.User.Id,
                Email = result.User.Email,
                DisplayName = result.User.DisplayName,
                Roles = result.User.Roles
            });

            _authState.NotifyUserAuthentication();

            return AuthResult.Ok();
        }
        public async Task RequestPasswordResetAsync(string email)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/auth/forgot-password",
                new ForgotPasswordRequestDto { Email = email });

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new Exception(errorText);
            }
        }

        public Task SignOutAsync()
        {
            _sessionStore.Clear();
            _authState.NotifyUserLogout();
            return Task.CompletedTask;
        }
    }
}