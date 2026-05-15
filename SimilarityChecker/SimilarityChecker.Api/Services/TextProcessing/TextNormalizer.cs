using System.Text;
using System.Text.RegularExpressions;

namespace SimilarityChecker.Api.Services.TextProcessing
{
    public static class TextNormalizer
    {
        private static readonly Regex MultiSpace = new(@"\s+", RegexOptions.Compiled);

        public static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var s = input.Normalize(NormalizationForm.FormKC);

            s = s.ToLowerInvariant();

            s = MultiSpace.Replace(s, " ").Trim();
            return s;
        }

        public static List<string> TokenizeWords(string normalizedText)
        {
            var tokens = Regex.Matches(normalizedText, @"\p{L}+")
                              .Select(m => m.Value)
                              .ToList();
            return tokens;
        }
    }
}
