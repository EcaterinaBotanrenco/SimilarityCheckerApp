namespace SimilarityChecker.Shared.Dtos
{
    public sealed class AuthResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Token { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public CurrentUserDto? User { get; set; }
    }
}