namespace SimilarityChecker.Api.Services.InternalScan
{
    public sealed class ExactMatchFragment
    {
        public int SourceTokenStart { get; set; }
        public int SourceTokenEnd { get; set; }
        public int RefTokenStart { get; set; }
        public int RefTokenEnd { get; set; }
        public double Score { get; set; }
        public string SourceSnippet { get; set; } = "";
        public string ReferenceSnippet { get; set; } = "";
    }
}
