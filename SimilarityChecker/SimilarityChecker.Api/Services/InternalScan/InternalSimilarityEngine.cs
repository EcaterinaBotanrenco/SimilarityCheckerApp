using System.Text.RegularExpressions;

namespace SimilarityChecker.Api.Services.InternalScan;

public static class InternalSimilarityEngine
{
    private const int ShingleSize = 7;

    public static double ComputeSimilarity(string a, string b)
    {
        var setA = BuildShingleSet(a);
        var setB = BuildShingleSet(b);

        if (setA.Count == 0 || setB.Count == 0) return 0;

        int intersection = 0;
        if (setA.Count > setB.Count) (setA, setB) = (setB, setA);

        foreach (var h in setA)
            if (setB.Contains(h)) intersection++;

        int union = setA.Count + setB.Count - intersection;
        return union == 0 ? 0 : (double)intersection / union;
    }

    // ✅ NOU: detectează fragmente copiate fix (Exact) cu poziții (token ranges)
    public static List<ExactMatchFragment> FindExactFragments(
        string sourceText,
        string referenceText,
        int minConsecutiveShingles = 3) // 3 shingles consecutive => fragment solid
    {
        var sourceTokens = Tokenize(sourceText);
        var refTokens = Tokenize(referenceText);

        if (sourceTokens.Length < ShingleSize || refTokens.Length < ShingleSize)
            return new();

        // index shingles pentru referință: hash -> listă poziții
        var refIndex = new Dictionary<ulong, List<int>>(capacity: refTokens.Length);

        for (int j = 0; j <= refTokens.Length - ShingleSize; j++)
        {
            var h = HashShingle(refTokens, j);
            if (!refIndex.TryGetValue(h, out var list))
            {
                list = new List<int>();
                refIndex[h] = list;
            }
            list.Add(j);
        }

        // găsim "puncte" de match: (i in source, j in ref)
        var points = new List<(int s, int r)>();

        for (int i = 0; i <= sourceTokens.Length - ShingleSize; i++)
        {
            var h = HashShingle(sourceTokens, i);
            if (refIndex.TryGetValue(h, out var refPositions))
            {
                // alegem prima poziție; pentru MVP e suficient
                points.Add((i, refPositions[0]));
            }
        }

        if (points.Count == 0) return new();

        // sortăm după source index
        points.Sort((a, b) => a.s.CompareTo(b.s));

        // unim puncte consecutive (s crește cu 1 și r crește cu 1)
        var fragments = new List<ExactMatchFragment>();
        int startS = points[0].s, startR = points[0].r;
        int prevS = startS, prevR = startR;
        int consecutive = 1;

        for (int k = 1; k < points.Count; k++)
        {
            var (s, r) = points[k];

            if (s == prevS + 1 && r == prevR + 1)
            {
                consecutive++;
                prevS = s;
                prevR = r;
                continue;
            }

            TryEmitFragment(sourceTokens, refTokens, startS, startR, prevS, prevR, consecutive,
                minConsecutiveShingles, fragments);

            startS = prevS = s;
            startR = prevR = r;
            consecutive = 1;
        }

        TryEmitFragment(sourceTokens, refTokens, startS, startR, prevS, prevR, consecutive,
            minConsecutiveShingles, fragments);

        return fragments;
    }

    private static void TryEmitFragment(
        string[] sourceTokens,
        string[] refTokens,
        int startS, int startR,
        int endS, int endR,
        int consecutiveShingles,
        int minConsecutiveShingles,
        List<ExactMatchFragment> outList)
    {
        if (consecutiveShingles < minConsecutiveShingles) return;

        // token range: de la start shingle până la end shingle + ShingleSize
        int sourceTokenStart = startS;
        int sourceTokenEnd = endS + ShingleSize; // end exclusiv

        int refTokenStart = startR;
        int refTokenEnd = endR + ShingleSize;

        outList.Add(new ExactMatchFragment
        {
            SourceTokenStart = sourceTokenStart,
            SourceTokenEnd = sourceTokenEnd,
            RefTokenStart = refTokenStart,
            RefTokenEnd = refTokenEnd,
            SourceSnippet = JoinTokens(sourceTokens, sourceTokenStart, sourceTokenEnd),
            ReferenceSnippet = JoinTokens(refTokens, refTokenStart, refTokenEnd),
            Score = 1.0 // exact
        });
    }

    private static ulong HashShingle(string[] tokens, int start)
    {
        // evităm string.Join ca să fie mai rapid
        // hash peste tokens[start..start+ShingleSize)
        const ulong offset = 14695981039346656037;
        const ulong prime = 1099511628211;

        ulong hash = offset;
        for (int k = 0; k < ShingleSize; k++)
        {
            var t = tokens[start + k];
            foreach (var ch in t)
            {
                hash ^= ch;
                hash *= prime;
            }
            hash ^= (ulong)' '; // separator
            hash *= prime;
        }
        return hash;
    }

    private static HashSet<ulong> BuildShingleSet(string text)
    {
        var tokens = Tokenize(text);
        var set = new HashSet<ulong>();
        if (tokens.Length < ShingleSize) return set;

        for (int i = 0; i <= tokens.Length - ShingleSize; i++)
            set.Add(HashShingle(tokens, i));

        return set;
    }

    private static string[] Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        var cleaned = Regex.Replace(text.ToLowerInvariant(), @"[^\p{L}\p{N}\s]+", " ");
        return cleaned.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static string JoinTokens(string[] tokens, int start, int endExclusive)
    {
        start = Math.Max(0, start);
        endExclusive = Math.Min(tokens.Length, endExclusive);
        if (endExclusive <= start) return string.Empty;
        return string.Join(' ', tokens, start, endExclusive - start);
    }

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