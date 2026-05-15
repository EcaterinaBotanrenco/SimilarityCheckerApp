namespace SimilarityChecker.Api.Services.InternalScan
{

    public sealed class ParaphraseMatchFragment
    {
        public int SourceTokenStart { get; set; }
        public int SourceTokenEnd { get; set; }
        public int RefTokenStart { get; set; }
        public int RefTokenEnd { get; set; }
        public int SharedContentWords { get; set; }
        public double Score { get; set; }
        public string SourceSnippet { get; set; } = "";
        public string ReferenceSnippet { get; set; } = "";
    }
}
