using SimilarityChecker.Shared.Dtos;
using SimilarityChecker.UI.Authentication;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SimilarityChecker.UI.Services
{
    public sealed class ProfileApiClient : IProfileApiClient
    {
        private readonly HttpClient _http;
        private readonly AuthSessionStore _sessionStore;

        public ProfileApiClient(HttpClient http, AuthSessionStore sessionStore)
        {
            _http = http;
            _sessionStore = sessionStore;
        }

        public async Task<UserProfileDto> GetMyProfileAsync(CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/profile/me");

            var token = _sessionStore.Session?.Token;
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<UserProfileDto>(cancellationToken: ct);
            return dto ?? throw new InvalidOperationException("Răspuns invalid de la server.");
        }
    }
}