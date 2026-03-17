using SimilarityChecker.Shared.Dto;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SimilarityChecker.UI.Services;

public interface IDocumentScanApiClient
{
    Task<DocumentUploadResponseDto> UploadDocumentAsync(Stream fileStream, string fileName, CancellationToken ct = default);
    Task<InternalScanStartResponseDto> StartInternalScanAsync(Guid documentId, CancellationToken ct = default);
    Task<InternalScanReportDto> GetInternalReportAsync(Guid documentId, CancellationToken ct = default);
    Task<InternalScanReportDto> CompareTwoDocumentsAsync(Guid primaryDocumentId, Guid referenceDocumentId, CancellationToken ct = default);
}
