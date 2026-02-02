using System;

namespace SimilarityChecker.UI.Authentication
{
    public sealed class AppUser
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = "";

        public string FirstName { get; init; } = "";
        public string LastName { get; init; } = "";

        public string DisplayName => $"{FirstName} {LastName}".Trim();

        public string PasswordHash { get; init; } = "";
        public string[] Roles { get; init; } = Array.Empty<string>();
    }
}
