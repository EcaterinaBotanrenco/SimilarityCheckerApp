using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimilarityChecker.Shared.Dto
{
    public interface IPlagiarismService
    {
        Task<PlagiarismCheckResponse> CheckAsync(PlagiarismCheckRequest request);

        // pentru PDF: obții din server datele pentru runId
        Task<PlagiarismResultDto?> GetResultAsync(Guid runId);
    }
}
