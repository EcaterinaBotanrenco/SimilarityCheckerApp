using System.ComponentModel.DataAnnotations;

namespace SimilarityChecker.Api.Data.Entities
{
    public sealed class PasswordResetTokenEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }

        [MaxLength(500)]
        public string TokenHash { get; set; } = "";

        public DateTime ExpiresAtUtc { get; set; }

        public bool IsUsed { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public AppUserEntity? User { get; set; }
    }
}