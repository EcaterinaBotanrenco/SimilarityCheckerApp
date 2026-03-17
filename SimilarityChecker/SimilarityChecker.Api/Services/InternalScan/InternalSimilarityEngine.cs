using System.Text.RegularExpressions;

namespace SimilarityChecker.Api.Services.InternalScan;

public static class InternalSimilarityEngine
{
    private const int ShingleSize = 7;

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "și","si","sau","iar","dar","de","din","la","cu","în","in","pe","pentru","prin","despre",
        "un","o","unul","una","unei","unui","ale","a","ai","al","alei","cel","cea","cei","cele",
        "este","sunt","fi","fie","fost","va","se","sa","să","că","ca","care","ce","acest","aceasta",
        "aceste","acestor","acel","acea","acele","acolo","aici","mai","mult","foarte","după","dupa",
        "the","and","or","of","to","in","on","for","with","from","is","are","was","were","be","been",
        "this","that","these","those","as","by","an","a"
    };

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

    public static List<ExactMatchFragment> FindExactFragments(
        string sourceText,
        string referenceText,
        int minConsecutiveShingles = 3)
    {
        var sourceTokens = Tokenize(sourceText);
        var refTokens = Tokenize(referenceText);

        if (sourceTokens.Length < ShingleSize || refTokens.Length < ShingleSize)
            return new();

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

        var points = new List<(int s, int r)>();

        for (int i = 0; i <= sourceTokens.Length - ShingleSize; i++)
        {
            var h = HashShingle(sourceTokens, i);
            if (refIndex.TryGetValue(h, out var refPositions))
            {
                points.Add((i, refPositions[0]));
            }
        }

        if (points.Count == 0) return new();

        points.Sort((a, b) => a.s.CompareTo(b.s));

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

    public static List<ParaphraseMatchFragment> FindParaphraseFragments(
    string sourceText,
    string referenceText,
    double minCombinedScore = 0.20,
    double maxExactSimilarity = 0.95,
    int minSharedContentWords = 1)
    {
        var sourceSentences = SplitIntoSentencesWithTokenRanges(sourceText);
        var referenceSentences = SplitIntoSentencesWithTokenRanges(referenceText);

        if (sourceSentences.Count == 0 || referenceSentences.Count == 0)
            return new();

        var results = new List<ParaphraseMatchFragment>();

        foreach (var sourceSentence in sourceSentences)
        {
            var sourceContentSet = BuildContentWordSet(sourceSentence.Text);
            if (sourceContentSet.Count == 0)
                continue;

            ParaphraseMatchFragment? best = null;

            foreach (var referenceSentence in referenceSentences)
            {
                var referenceContentSet = BuildContentWordSet(referenceSentence.Text);
                if (referenceContentSet.Count == 0)
                    continue;

                int sharedWords = CountIntersection(sourceContentSet, referenceContentSet);
                if (sharedWords < minSharedContentWords)
                    continue;

                double wordScore = JaccardSimilarity(sourceContentSet, referenceContentSet);
                double charScore = DiceBigramsSimilarity(sourceSentence.Text, referenceSentence.Text);
                double exactScore = ComputeWindowExactSimilarity(
                    sourceSentence.Tokens, 0, sourceSentence.Tokens.Length,
                    referenceSentence.Tokens, 0, referenceSentence.Tokens.Length);

                if (exactScore >= maxExactSimilarity)
                    continue;

                double combinedScore = 0.80 * wordScore + 0.20 * charScore;

                if (combinedScore < minCombinedScore)
                    continue;

                if (best is null || combinedScore > best.Score)
                {
                    best = new ParaphraseMatchFragment
                    {
                        SourceTokenStart = sourceSentence.GlobalStartToken,
                        SourceTokenEnd = sourceSentence.GlobalEndToken,
                        RefTokenStart = referenceSentence.GlobalStartToken,
                        RefTokenEnd = referenceSentence.GlobalEndToken,
                        SharedContentWords = sharedWords,
                        Score = combinedScore,
                        SourceSnippet = sourceSentence.Text,
                        ReferenceSnippet = referenceSentence.Text
                    };
                }
            }

            if (best is not null)
                results.Add(best);
        }

        return MergeParaphraseFragments(results, 1);
    }

    private static List<ParaphraseMatchFragment> MergeParaphraseFragments(
        List<ParaphraseMatchFragment> fragments,
        int stepTokens)
    {
        if (fragments.Count == 0)
            return fragments;

        var ordered = fragments
            .OrderBy(f => f.SourceTokenStart)
            .ThenBy(f => f.RefTokenStart)
            .ToList();

        var merged = new List<ParaphraseMatchFragment>();
        var current = Clone(ordered[0]);

        for (int i = 1; i < ordered.Count; i++)
        {
            var next = ordered[i];

            bool sameFlow =
                next.SourceTokenStart <= current.SourceTokenEnd + stepTokens &&
                next.RefTokenStart <= current.RefTokenEnd + stepTokens;

            if (sameFlow)
            {
                current.SourceTokenEnd = Math.Max(current.SourceTokenEnd, next.SourceTokenEnd);
                current.RefTokenEnd = Math.Max(current.RefTokenEnd, next.RefTokenEnd);
                current.Score = Math.Max(current.Score, next.Score);
                current.SharedContentWords = Math.Max(current.SharedContentWords, next.SharedContentWords);
            }
            else
            {
                merged.Add(current);
                current = Clone(next);
            }
        }

        merged.Add(current);
        return merged;
    }

    private static List<SentenceChunk> SplitIntoSentencesWithTokenRanges(string text)
    {
        var result = new List<SentenceChunk>();

        if (string.IsNullOrWhiteSpace(text))
            return result;

        var matches = Regex.Matches(text, @"[^.!?\r\n]+[.!?]?");
        int globalTokenIndex = 0;

        foreach (Match match in matches)
        {
            var sentenceText = match.Value.Trim();
            if (string.IsNullOrWhiteSpace(sentenceText))
                continue;

            var tokens = Tokenize(sentenceText);
            if (tokens.Length == 0)
                continue;

            result.Add(new SentenceChunk
            {
                Text = sentenceText,
                Tokens = tokens,
                GlobalStartToken = globalTokenIndex,
                // keep end as exclusive index to be consistent with other token ranges
                GlobalEndToken = globalTokenIndex + tokens.Length
            });

            globalTokenIndex += tokens.Length;
        }

        return result;
    }

    private static HashSet<string> BuildContentWordSet(string text)
    {
        var tokens = Tokenize(text);
        var result = new HashSet<string>();

        foreach (var token in tokens)
        {
            var t = NormalizeContentToken(token);

            if (string.IsNullOrWhiteSpace(t))
                continue;

            if (t.Length < 3)
                continue;

            if (StopWords.Contains(t))
                continue;

            result.Add(t);
        }

        return result;
    }

    private static double JaccardSimilarity(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count == 0 || b.Count == 0)
            return 0;

        int intersection = CountIntersection(a, b);
        int union = a.Count + b.Count - intersection;

        return union == 0 ? 0 : (double)intersection / union;
    }

    private static double DiceBigramsSimilarity(string a, string b)
    {
        var setA = BuildCharacterBigrams(a);
        var setB = BuildCharacterBigrams(b);

        if (setA.Count == 0 || setB.Count == 0)
            return 0;

        int intersection = CountIntersection(setA, setB);
        return (2.0 * intersection) / (setA.Count + setB.Count);
    }

    private static HashSet<string> BuildCharacterBigrams(string text)
    {
        var cleaned = Regex.Replace(text.ToLowerInvariant(), @"\s+", " ").Trim();
        var result = new HashSet<string>();

        if (cleaned.Length < 2)
            return result;

        for (int i = 0; i < cleaned.Length - 1; i++)
        {
            result.Add(cleaned.Substring(i, 2));
        }

        return result;
    }

    private sealed class SentenceChunk
    {
        public string Text { get; set; } = "";
        public string[] Tokens { get; set; } = Array.Empty<string>();
        public int GlobalStartToken { get; set; }
        public int GlobalEndToken { get; set; }
    }

    private static ParaphraseMatchFragment Clone(ParaphraseMatchFragment x)
    {
        return new ParaphraseMatchFragment
        {
            SourceTokenStart = x.SourceTokenStart,
            SourceTokenEnd = x.SourceTokenEnd,
            RefTokenStart = x.RefTokenStart,
            RefTokenEnd = x.RefTokenEnd,
            Score = x.Score,
            SharedContentWords = x.SharedContentWords,
            SourceSnippet = x.SourceSnippet,
            ReferenceSnippet = x.ReferenceSnippet
        };
    }

    private static int CountIntersection<T>(HashSet<T> a, HashSet<T> b)
    {
        if (a.Count > b.Count)
            (a, b) = (b, a);

        int count = 0;
        foreach (var x in a)
            if (b.Contains(x))
                count++;

        return count;
    }

    private static HashSet<string> BuildContentTokenSet(string[] tokens, int start, int endExclusive)
    {
        var result = new HashSet<string>();

        for (int i = start; i < endExclusive && i < tokens.Length; i++)
        {
            var t = NormalizeContentToken(tokens[i]);
            if (string.IsNullOrWhiteSpace(t))
                continue;

            if (t.Length < 3)
                continue;

            if (StopWords.Contains(t))
                continue;

            result.Add(t);
        }

        return result;
    }

    private static string NormalizeContentToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return string.Empty;

        token = token.Trim().ToLowerInvariant();

        if (token.Length <= 4)
            return token;

        string[] suffixes =
        {
            "ului","elor","ilor","area","irea","iile","iilor","ație","atie","isme",
            "ități","itati","ilor","elor","ilor","ilor","ția","tia","țiile","tiile",
            "ing","ment","ments","tion","tions","ness","less","able","ibil","ibilă",
            "ului","ului","ilor","elor","ate","ite","ului","ului","ilor",
            "ul","ului","ele","ile","lor","ii","ie","ia","a","e","i"
        };

        foreach (var suffix in suffixes.OrderByDescending(x => x.Length))
        {
            if (token.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) &&
                token.Length > suffix.Length + 3)
            {
                return token[..^suffix.Length];
            }
        }

        return token;
    }

    private static double ComputeWindowExactSimilarity(
        string[] sourceTokens, int sourceStart, int sourceEnd,
        string[] refTokens, int refStart, int refEnd)
    {
        var sourceSlice = SliceTokens(sourceTokens, sourceStart, sourceEnd);
        var refSlice = SliceTokens(refTokens, refStart, refEnd);

        if (sourceSlice.Length < 4 || refSlice.Length < 4)
            return 0;

        var setA = BuildShingleSetFromTokens(sourceSlice, 4);
        var setB = BuildShingleSetFromTokens(refSlice, 4);

        if (setA.Count == 0 || setB.Count == 0)
            return 0;

        int intersection = CountIntersection(setA, setB);
        int union = setA.Count + setB.Count - intersection;
        return union == 0 ? 0 : (double)intersection / union;
    }

    private static string[] SliceTokens(string[] tokens, int start, int endExclusive)
    {
        start = Math.Max(0, start);
        endExclusive = Math.Min(tokens.Length, endExclusive);

        if (endExclusive <= start)
            return Array.Empty<string>();

        var result = new string[endExclusive - start];
        Array.Copy(tokens, start, result, 0, result.Length);
        return result;
    }

    private static HashSet<string> BuildShingleSetFromTokens(string[] tokens, int size)
    {
        var set = new HashSet<string>();
        if (tokens.Length < size) return set;

        for (int i = 0; i <= tokens.Length - size; i++)
            set.Add(string.Join(' ', tokens, i, size));

        return set;
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

        int sourceTokenStart = startS;
        int sourceTokenEnd = endS + ShingleSize;

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
            Score = 1.0
        });
    }

    private static ulong HashShingle(string[] tokens, int start)
    {
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
            hash ^= (ulong)' ';
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