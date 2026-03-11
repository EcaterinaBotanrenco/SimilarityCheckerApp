using Microsoft.AspNetCore.Components.Forms;
using SimilarityChecker.Shared.Dto;
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
    public sealed class PlagiarismApiClient : IPlagiarismApiClient
    {
        private readonly HttpClient _http;
        private readonly AuthSessionStore _sessionStore;

        public PlagiarismApiClient(HttpClient http, AuthSessionStore sessionStore)
        {
            _http = http;
            _sessionStore = sessionStore;
        }

        public async Task<PlagiarismCheckResponse> CheckAsync(
            IBrowserFile mainFile,
            IBrowserFile? referenceFile,
            CancellationToken ct = default)
        {
            using var form = new MultipartFormDataContent();

            var mainStream = mainFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            var mainContent = new StreamContent(mainStream);
            mainContent.Headers.ContentType = new MediaTypeHeaderValue(mainFile.ContentType);
            form.Add(mainContent, "mainFile", mainFile.Name);

            if (referenceFile is not null)
            {
                var refStream = referenceFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                var refContent = new StreamContent(refStream);
                refContent.Headers.ContentType = new MediaTypeHeaderValue(referenceFile.ContentType);
                form.Add(refContent, "referenceFile", referenceFile.Name);
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/plagiarism/check")
            {
                Content = form
            };

            var token = _sessionStore.Session?.Token;
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await _http.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException($"Eroare API ({response.StatusCode}): {err}");
            }

            var result = await response.Content.ReadFromJsonAsync<PlagiarismCheckResponse>(cancellationToken: ct);
            return result!;
        }
    }
}