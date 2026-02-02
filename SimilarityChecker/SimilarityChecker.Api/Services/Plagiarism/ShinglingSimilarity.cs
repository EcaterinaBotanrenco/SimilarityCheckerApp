namespace SimilarityChecker.Api.Services.Plagiarism
{
    public static class ShinglingSimilarity
    {
        public static HashSet<string> BuildWordShingles(List<string> tokens, int shingleSize)
        {
            var set = new HashSet<string>();
            if (tokens.Count < shingleSize) return set;

            for (int i = 0; i <= tokens.Count - shingleSize; i++)
            {
                var shingle = string.Join(' ', tokens.Skip(i).Take(shingleSize));
                set.Add(shingle);
            }
            return set;
        }

        public static int JaccardPercent(HashSet<string> a, HashSet<string> b)
        {
            if (a.Count == 0 || b.Count == 0) return 0;

            int inter = a.Intersect(b).Count();
            int uni = a.Union(b).Count();

            // 0..100
            var score = (int)Math.Round(100.0 * inter / uni);
            return Math.Clamp(score, 0, 100);
        }
    }
}
