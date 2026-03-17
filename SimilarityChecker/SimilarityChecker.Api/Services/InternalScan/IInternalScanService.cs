using SimilarityChecker.Api.Models;
using SimilarityChecker.Shared.Dto;

namespace SimilarityChecker.Api.Services.InternalScan;

public interface IInternalScanService
{
    Task<InternalScanStartResponse> StartAsync(Guid documentId, double threshold, CancellationToken ct);
    Task<InternalScanReportDto?> GetReportAsync(Guid documentId, CancellationToken ct);
    Task<InternalScanReportDto> CompareTwoDocumentsAsync(
        Guid primaryDocumentId, 
        Guid referenceDocumentId, 
        double threshold, 
        CancellationToken ct = default);

}