using System.ComponentModel.DataAnnotations;

namespace SimilarityChecker.Api.Data.Entities;

public sealed class SearchQueryEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocumentId { get; set; }
    public DocumentEntity Document { get; set; } = null!;

    [MaxLength(500)]
    public string QueryText { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<SearchResultEntity> Results { get; set; } = new();
}

