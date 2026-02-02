using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimilarityChecker.UI.Authentication
{
    public sealed class InMemoryUserStore : IUserStore
    {
        private readonly List<AppUser> _users;

        public InMemoryUserStore()
        {
            // Demo: user = admin@plagiat.md, parola = Admin123
            _users = new List<AppUser>
        {
            new AppUser
            {
                Id = Guid.NewGuid(),
                Email = "admin@plagiat.md",
                FirstName = "Administrator",
                LastName = "",
                PasswordHash = PasswordHasher.Hash("Admin123"),
                Roles = new[] { "Admin" }
            }
        };
        }

        public Task<AppUser?> FindByEmailAsync(string email)
        {
            var u = _users.FirstOrDefault(x => x.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(u);
        }
        public Task<bool> EmailExistsAsync(string email)
        {
            var exists = _users.Any(x => x.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }

        public Task CreateAsync(AppUser user)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }
    }
}
