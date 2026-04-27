using SimilarityChecker.Shared.Dto;
using SimilarityChecker.Shared.Dtos;
using SimilarityChecker.UI.Authentication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

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

        private static async Task EnsureSuccessWithMessageAsync(HttpResponseMessage response, string fallbackMessage)
        {
            if (response.IsSuccessStatusCode)
                return;

            var serverMessage = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(serverMessage))
            {
                serverMessage = serverMessage.Trim().Trim('"');
                throw new InvalidOperationException(serverMessage);
            }

            throw new InvalidOperationException(fallbackMessage);
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
            await EnsureSuccessWithMessageAsync(response, "Încărcarea documentului a eșuat.");

            var dto = await response.Content.ReadFromJsonAsync<DocumentUploadResponseDto>(cancellationToken: ct);
            return dto ?? throw new InvalidOperationException("Răspuns invalid de la server (upload).");
        }

        public async Task<InternalScanStartResponseDto> StartInternalScanAsync(Guid documentId, CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/internal-scan/start?documentId={documentId}");
            AddAuthorizationHeader(request);

            var response = await _http.SendAsync(request, ct);
            await EnsureSuccessWithMessageAsync(response, "Pornirea scanării a eșuat.");

            var dto = await response.Content.ReadFromJsonAsync<InternalScanStartResponseDto>(cancellationToken: ct);
            return dto ?? throw new InvalidOperationException("Răspuns invalid de la server (start scan).");
        }

        public async Task<InternalScanReportDto> GetInternalReportAsync(Guid documentId, CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/internal-scan/report/{documentId}");
            AddAuthorizationHeader(request);

            var response = await _http.SendAsync(request, ct);
            await EnsureSuccessWithMessageAsync(response, "Raportul nu a putut fi încărcat.");

            var dto = await response.Content.ReadFromJsonAsync<InternalScanReportDto>(cancellationToken: ct);
            return dto ?? throw new InvalidOperationException("Răspuns invalid de la server (report).");
        }

        public async Task<InternalScanReportDto> CompareTwoDocumentsAsync(Guid primaryDocumentId, Guid referenceDocumentId, CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/internal-scan/compare?primaryDocumentId={primaryDocumentId}&referenceDocumentId={referenceDocumentId}");

            AddAuthorizationHeader(request);

            var response = await _http.SendAsync(request, ct);
            await EnsureSuccessWithMessageAsync(response, "Compararea documentelor a eșuat.");

            var dto = await response.Content.ReadFromJsonAsync<InternalScanReportDto>(cancellationToken: ct);
            return dto ?? throw new InvalidOperationException("Răspuns invalid de la server (compare documents).");
        }

        public async Task<MultipleDocumentUploadResponseDto> UploadMultipleDocumentsAsync(List<UploadFileItemDto> files, CancellationToken ct = default)
        {
            using var form = new MultipartFormDataContent();

            foreach (var file in files)
            {
                var fileContent = new ByteArrayContent(file.Content);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                form.Add(fileContent, "files", file.FileName);
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/documents/upload-multiple")
            {
                Content = form
            };

            AddAuthorizationHeader(request);

            var response = await _http.SendAsync(request, ct);
            await EnsureSuccessWithMessageAsync(response, "Încărcarea multiplă a documentelor a eșuat.");

            var dto = await response.Content.ReadFromJsonAsync<MultipleDocumentUploadResponseDto>(cancellationToken: ct);
            return dto ?? throw new InvalidOperationException("Răspuns invalid de la server (upload multiple).");
        }
        public async Task<InternalScanReportDto> ScanTextAsync(TextScanRequestDto requestDto, CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/internal-scan/text")
            {
                Content = JsonContent.Create(requestDto)
            };

            AddAuthorizationHeader(request);

            var response = await _http.SendAsync(request, ct);
            await EnsureSuccessWithMessageAsync(response, "Verificarea textului a eșuat.");

            var dto = await response.Content.ReadFromJsonAsync<InternalScanReportDto>(cancellationToken: ct);

            return dto ?? throw new InvalidOperationException("Răspuns invalid de la server (scan text).");
        }
    }
}