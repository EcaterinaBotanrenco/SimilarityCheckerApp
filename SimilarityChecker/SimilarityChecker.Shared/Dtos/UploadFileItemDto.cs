namespace SimilarityChecker.Shared.Dto;

public sealed class UploadFileItemDto
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}