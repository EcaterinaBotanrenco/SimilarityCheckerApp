namespace SimilarityChecker.Api.Services.Storage;

public interface IDocumentStorageService
{
    Task<(string RelativePath, long FileSizeBytes)> SaveOriginalFileAsync(
        Guid documentId,
        string originalFileName,
        byte[] content,
        CancellationToken ct = default);

    Task<string> SaveExtractedTextAsync(
        Guid documentId,
        string extractedText,
        CancellationToken ct = default);

    Task<string> ReadExtractedTextAsync(
        string relativePath,
        CancellationToken ct = default);
}