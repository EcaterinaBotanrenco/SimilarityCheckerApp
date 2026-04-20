using System;

namespace SimilarityChecker.Api.Data.Entities
{
    public class AppUserRoleEntity
    {
        public Guid UserId { get; set; }
        public AppUserEntity User { get; set; }

        public Guid RoleId { get; set; }
        public RoleEntity Role { get; set; }

        // Definim cheia primară compusă
        public Guid Id => UserId; // sau poți lăsa gol pentru a fi generat automat de EF
    }
}