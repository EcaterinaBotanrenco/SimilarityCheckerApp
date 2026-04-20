using Microsoft.EntityFrameworkCore;
using SimilarityChecker.Api.Data.Entities;

namespace SimilarityChecker.Api.Data
{
    public class Seeder
    {
        private readonly SimilarityCheckerDbContext _db;

        public Seeder(SimilarityCheckerDbContext context)
        {
            _db = context;
        }

        public async Task AssignAdminRoleToUsers()
        {
            // Găsește rolul Admin
            var adminRole = await _db.Set<RoleEntity>().FirstOrDefaultAsync(r => r.Name == "Admin");

            if (adminRole == null)
            {
                // Dacă rolul Admin nu există, îl adăugăm
                adminRole = new RoleEntity { Name = "Admin" };
                _db.Set<RoleEntity>().Add(adminRole);
                await _db.SaveChangesAsync();
            }

            // Găsește utilizatorii pe care dorim să le atribuim rolul Admin
            var usersToAssignRole = await _db.AppUsers
                .Where(u => u.Email == "cateabotnarenco@gmail.com")
                .ToListAsync();

            // Atribuim rolul Admin pentru acești utilizatori
            foreach (var user in usersToAssignRole)
            {
                // Verificăm dacă relația deja există
                var userRole = await _db.Set<AppUserRoleEntity>()
                    .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == adminRole.Id);

                if (userRole == null)
                {
                    // Adăugăm relația între utilizator și rol
                    _db.Set<AppUserRoleEntity>().Add(new AppUserRoleEntity
                    {
                        UserId = user.Id,
                        RoleId = adminRole.Id
                    });
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}
