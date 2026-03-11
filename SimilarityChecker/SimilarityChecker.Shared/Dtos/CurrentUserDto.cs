namespace SimilarityChecker.Shared.Dtos
{
    public sealed class CurrentUserDto
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string[] Roles { get; set; } = Array.Empty<string>();
    }
}