using System;

namespace SimilarityChecker.UI.Authentication
{
    public sealed class AuthSessionModel
    {
        public string Token { get; set; } = "";
        public string UserId { get; set; } = "";
        public string Email { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string[] Roles { get; set; } = System.Array.Empty<string>();
    }
}