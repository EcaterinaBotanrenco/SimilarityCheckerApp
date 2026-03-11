using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimilarityChecker.Shared.Dtos
{
    public sealed class UserReportItemDto
    {
        public Guid ReportId { get; set; }
        public Guid DocumentId { get; set; }

        public string FileName { get; set; } = "";
        public DateTime GeneratedAtUtc { get; set; }

        public string Status { get; set; } = "";
        public int ExactPercent { get; set; }
        public int ParaphrasePercent { get; set; }
        public int CleanPercent { get; set; }
    }
}
