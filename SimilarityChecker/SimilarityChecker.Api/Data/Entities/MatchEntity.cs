using System.ComponentModel.DataAnnotations;
using SimilarityChecker.Api.Data.Enums;
using MatchType = SimilarityChecker.Api.Data.Enums.MatchType;

namespace SimilarityChecker.Api.Data.Entities;

public sealed class MatchEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocumentId { get; set; }
    public DocumentEntity Document { get; set; } = null!;

    public Guid OnlineSourceId { get; set; }
    public OnlineSourceEntity OnlineSource { get; set; } = null!;

    public MatchType MatchType { get; set; }

    // Offset-uri (pentru highlight în raport)
    public int DocStart { get; set; }
    public int DocEnd { get; set; }

    public int SourceStart { get; set; }
    public int SourceEnd { get; set; }

    // Scor 0..1
    public double Score { get; set; }

    [MaxLength(50)]
    public string AlgorithmVersion { get; set; } = "v1";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
