using System.ComponentModel.DataAnnotations;

namespace SimilarityChecker.Api.Data.Entities;

public sealed class SearchResultEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SearchQueryId { get; set; }
    public SearchQueryEntity SearchQuery { get; set; } = null!;

    public int Rank { get; set; }

    [MaxLength(2048)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Title { get; set; }

    [MaxLength(1000)]
    public string? Snippet { get; set; }
}
