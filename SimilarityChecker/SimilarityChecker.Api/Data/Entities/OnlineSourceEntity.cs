using System.ComponentModel.DataAnnotations;

namespace SimilarityChecker.Api.Data.Entities;

public sealed class OnlineSourceEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(2048)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(260)]
    public string Domain { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Title { get; set; }

    public DateTime RetrievedAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(64)]
    public string? ContentHash { get; set; }

    public int? StatusCode { get; set; }

    public string ExtractedText { get; set; } = string.Empty;

    // Navigații
    public List<MatchEntity> Matches { get; set; } = new();
}
