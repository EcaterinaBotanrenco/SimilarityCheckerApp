using Microsoft.AspNetCore.Mvc;
using SimilarityChecker.Api.Models;
using SimilarityChecker.Shared.Dto;

namespace SimilarityChecker.Api.Controllers;

[ApiController]
[Route("api/plagiarism")]
public sealed class PlagiarismController : ControllerBase
{
    private readonly IPlagiarismService _plagiarismService;

    public PlagiarismController(IPlagiarismService plagiarismService)
    {
        _plagiarismService = plagiarismService;
    }

    [HttpPost("check")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PlagiarismCheckResponse>> Check(
    [FromForm] PlagiarismUploadRequest request,
    CancellationToken ct)
    {
        if (request.MainFile == null || request.MainFile.Length == 0)
            return BadRequest("Documentul principal este obligatoriu.");

        byte[] mainBytes;
        byte[]? refBytes = null;

        using (var ms = new MemoryStream())
        {
            await request.MainFile.CopyToAsync(ms, ct);
            mainBytes = ms.ToArray();
        }

        if (request.ReferenceFile != null && request.ReferenceFile.Length > 0)
        {
            using var ms = new MemoryStream();
            await request.ReferenceFile.CopyToAsync(ms, ct);
            refBytes = ms.ToArray();
        }

        var checkRequest = new PlagiarismCheckRequest
        {
            MainFileName = request.MainFile.FileName,
            MainContent = mainBytes,
            ReferenceFileName = request.ReferenceFile?.FileName,
            ReferenceContent = refBytes
        };

        var result = await _plagiarismService.CheckAsync(checkRequest);
        return Ok(result);
    }

}
