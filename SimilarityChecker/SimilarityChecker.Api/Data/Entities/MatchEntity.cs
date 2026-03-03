using SimilarityChecker.Api.Data.Entities;

public sealed class MatchEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocumentId { get; set; }
    public DocumentEntity Document { get; set; } = null!;

    public Guid? OnlineSourceId { get; set; }
    public OnlineSourceEntity? OnlineSource { get; set; }

    public Guid? ComparedDocumentId { get; set; }
    public DocumentEntity? ComparedDocument { get; set; }

    public MatchType MatchType { get; set; }

    public int DocStart { get; set; }
    public int DocEnd { get; set; }

    public int SourceStart { get; set; }
    public int SourceEnd { get; set; }

    public double Score { get; set; }
    public string AlgorithmVersion { get; set; } = "v1";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
