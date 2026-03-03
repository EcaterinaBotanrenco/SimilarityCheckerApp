using SimilarityChecker.Shared.Dtos;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SimilarityChecker.UI.Services;

public sealed class DocumentScanApiClient : IDocumentScanApiClient
{
    private readonly HttpClient _http;

    public DocumentScanApiClient(HttpClient http) => _http = http;

    public async Task<DocumentUploadResponseDto> UploadDocumentAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();

        var fileContent = new StreamContent(fileStream);
        // (opțional) content-type:
        // fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        content.Add(fileContent, "File", fileName); // numele "File" trebuie să corespundă cu DocumentUploadRequest.File

        var resp = await _http.PostAsync("api/documents/upload", content, ct);
        resp.EnsureSuccessStatusCode();

        var dto = await resp.Content.ReadFromJsonAsync<DocumentUploadResponseDto>(cancellationToken: ct);
        return dto ?? throw new InvalidOperationException("Răspuns invalid de la server (upload).");
    }

    public async Task<InternalScanStartResponseDto> StartInternalScanAsync(Guid documentId, CancellationToken ct = default)
    {
        var url = $"api/internal-scan/start?documentId={documentId}";
        var resp = await _http.PostAsync(url, content: null, ct);
        resp.EnsureSuccessStatusCode();

        var dto = await resp.Content.ReadFromJsonAsync<InternalScanStartResponseDto>(cancellationToken: ct);
        return dto ?? throw new InvalidOperationException("Răspuns invalid de la server (start scan).");
    }

    public async Task<InternalScanReportDto> GetInternalReportAsync(Guid documentId, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"api/internal-scan/report/{documentId}", ct);
        resp.EnsureSuccessStatusCode();

        var dto = await resp.Content.ReadFromJsonAsync<InternalScanReportDto>(cancellationToken: ct);
        return dto ?? throw new InvalidOperationException("Răspuns invalid de la server (report).");
    }
}
