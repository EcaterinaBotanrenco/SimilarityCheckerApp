namespace SimilarityChecker.Api.Models;

public sealed class InternalScanStartResponse
{
    public Guid ReportId { get; set; }
    public Guid DocumentId { get; set; }
    public int ComparedDocuments { get; set; }
}
