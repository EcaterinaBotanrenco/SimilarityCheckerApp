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

            // Normalize Unicode (diacritice, forme compuse)
            var s = input.Normalize(NormalizationForm.FormKC);

            // Lowercase invariant (funcționează ok pt RO/RU/EN)
            s = s.ToLowerInvariant();

            // Înlocuim whitespace multiplu
            s = MultiSpace.Replace(s, " ").Trim();
            return s;
        }

        // Tokenizare pe litere (include RO/RU/EN)
        public static List<string> TokenizeWords(string normalizedText)
        {
            // extrage secvențe de litere (nu numere/punctuație)
            var tokens = Regex.Matches(normalizedText, @"\p{L}+")
                              .Select(m => m.Value)
                              .ToList();
            return tokens;
        }
    }
}
