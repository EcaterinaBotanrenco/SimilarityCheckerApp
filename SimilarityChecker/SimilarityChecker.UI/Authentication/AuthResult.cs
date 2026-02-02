namespace SimilarityChecker.UI.Authentication
{
    public sealed class AuthResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }

        public static AuthResult Ok() => new() { Success = true };
        public static AuthResult Fail(string message) => new() { Success = false, ErrorMessage = message };
    }
}
