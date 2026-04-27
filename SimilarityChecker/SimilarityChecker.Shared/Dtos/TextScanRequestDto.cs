using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimilarityChecker.Shared.Dto;

public sealed class TextScanRequestDto
{
    public string? Title { get; set; }

    public string Text { get; set; } = string.Empty;
}
