using Microsoft.AspNetCore.Components.Forms;
using SimilarityChecker.Shared.Dto;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SimilarityChecker.UI.Services;

public sealed class PlagiarismApiClient : IPlagiarismApiClient
{
    private readonly HttpClient _http;

    public PlagiarismApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<PlagiarismCheckResponse> CheckAsync(
        IBrowserFile mainFile,
        IBrowserFile? referenceFile,
        CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();

        // ===== main document =====
        var mainStream = mainFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
        var mainContent = new StreamContent(mainStream);
        mainContent.Headers.ContentType =
            new MediaTypeHeaderValue(mainFile.ContentType);

        form.Add(mainContent, "mainFile", mainFile.Name);

        // ===== reference document (optional) =====
        if (referenceFile is not null)
        {
            var refStream = referenceFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            var refContent = new StreamContent(refStream);
            refContent.Headers.ContentType =
                new MediaTypeHeaderValue(referenceFile.ContentType);

            form.Add(refContent, "referenceFile", referenceFile.Name);
        }

        var response = await _http.PostAsync(
            "api/plagiarism/check",
            form,
            ct);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Eroare API ({response.StatusCode}): {err}");
        }

        var result = await response.Content
            .ReadFromJsonAsync<PlagiarismCheckResponse>(cancellationToken: ct);

        return result!;
    }
}
