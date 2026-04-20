namespace SimilarityChecker.Api.Data.Entities
{
    public class RoleEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;

        public List<AppUserRoleEntity> UserRoles { get; set; } = new List<AppUserRoleEntity>();
    }
}
