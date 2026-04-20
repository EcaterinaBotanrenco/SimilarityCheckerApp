using System.Text;

namespace SimilarityChecker.Api.Services.Storage;

public sealed class DocumentStorageService : IDocumentStorageService
{
    private readonly string _rootPath;

    public DocumentStorageService(IWebHostEnvironment env)
    {
        _rootPath = Path.Combine(env.ContentRootPath, "StoredDocuments");
    }

    public async Task<(string RelativePath, long FileSizeBytes)> SaveOriginalFileAsync(
        Guid documentId,
        string originalFileName,
        byte[] content,
        CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        var safeName = Path.GetFileNameWithoutExtension(originalFileName);
        safeName = SanitizeFileName(safeName);

        var folder = Path.Combine(_rootPath, "originals", DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
        Directory.CreateDirectory(folder);

        var fileName = $"{documentId}_{safeName}{ext}";
        var absolutePath = Path.Combine(folder, fileName);

        await File.WriteAllBytesAsync(absolutePath, content, ct);

        var relativePath = Path.GetRelativePath(_rootPath, absolutePath).Replace("\\", "/");
        return (relativePath, content.LongLength);
    }

    public async Task<string> SaveExtractedTextAsync(
        Guid documentId,
        string extractedText,
        CancellationToken ct = default)
    {
        var folder = Path.Combine(_rootPath, "extracted", DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
        Directory.CreateDirectory(folder);

        var fileName = $"{documentId}.txt";
        var absolutePath = Path.Combine(folder, fileName);

        await File.WriteAllTextAsync(absolutePath, extractedText ?? string.Empty, Encoding.UTF8, ct);

        return Path.GetRelativePath(_rootPath, absolutePath).Replace("\\", "/");
    }

    public async Task<string> ReadExtractedTextAsync(string relativePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return string.Empty;

        var absolutePath = Path.Combine(_rootPath, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

        if (!File.Exists(absolutePath))
            return string.Empty;

        return await File.ReadAllTextAsync(absolutePath, Encoding.UTF8, ct);
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }

        return string.IsNullOrWhiteSpace(fileName) ? "document" : fileName;
    }
}