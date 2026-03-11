using System.ComponentModel.DataAnnotations;

namespace SimilarityChecker.Api.Data.Entities
{
    public sealed class AppUserEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(256)]
        public string Email { get; set; } = "";

        [MaxLength(100)]
        public string FirstName { get; set; } = "";

        [MaxLength(100)]
        public string LastName { get; set; } = "";

        public string PasswordHash { get; set; } = "";

        [MaxLength(200)]
        public string RolesCsv { get; set; } = "";

        public List<DocumentEntity> Documents { get; set; } = new();
        public List<ReportEntity> Reports { get; set; } = new();
    }
}