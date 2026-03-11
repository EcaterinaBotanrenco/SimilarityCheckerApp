using SimilarityChecker.Shared.Dto;
using SimilarityChecker.Shared.Dtos;
using SimilarityChecker.UI.Authentication;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SimilarityChecker.UI.Services
{
    public sealed class DocumentScanApiClient : IDocumentScanApiClient
    {
        private readonly HttpClient _http;
        private readonly AuthSessionStore _sessionStore;

        public DocumentScanApiClient(HttpClient http, AuthSessionStore sessionStore)
        {
            _http = http;
            _sessionStore = sessionStore;
        }

        private void AddAuthorizationHeader(HttpRequestMessage request)
        {
            var token = _sessionStore.Session?.Token;
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<DocumentUploadResponseDto> UploadDocumentAsync(Stream fileStream, string fileName, CancellationToken ct = default)
        {
            using var form = new MultipartFormDataContent();
            using var fileContent = new StreamContent(fileStream);

            form.Add(fileContent, "File", fileName);

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/documents/upload")
            {
                Content = form
            };

            AddAuthorizationHeader(request);

            var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<DocumentUploadResponseDto>(cancellationToken: ct);
            return dto ?? throw new InvalidOperationException("Răspuns invalid de la server (upload).");
        }

        public async Task<InternalScanStartResponseDto> StartInternalScanAsync(Guid documentId, CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/internal-scan/start?documentId={documentId}");
            AddAuthorizationHeader(request);

            var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<InternalScanStartResponseDto>(cancellationToken: ct);
            return dto ?? throw new InvalidOperationException("Răspuns invalid de la server (start scan).");
        }

        public async Task<InternalScanReportDto> GetInternalReportAsync(Guid documentId, CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/internal-scan/report/{documentId}");
            AddAuthorizationHeader(request);

            var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<InternalScanReportDto>(cancellationToken: ct);
            return dto ?? throw new InvalidOperationException("Răspuns invalid de la server (report).");
        }
    }
}