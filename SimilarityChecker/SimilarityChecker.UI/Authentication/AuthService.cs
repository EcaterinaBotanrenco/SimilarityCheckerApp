using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SimilarityChecker.UI.Authentication
{
    public sealed class AuthService : IAuthService
    {
        private readonly IUserStore _users;
        private readonly IHttpContextAccessor _http;
        private readonly CustomAuthStateProvider _authState;

        public AuthService(IUserStore users, IHttpContextAccessor http, CustomAuthStateProvider authState)
        {
            _users = users;
            _http = http;
            _authState = authState;
        }

        public async Task<AuthResult> SignInAsync(string email, string password, bool rememberMe)
        {
            var user = await _users.FindByEmailAsync(email);
            if (user is null)
                return AuthResult.Fail("Utilizator inexistent.");

            if (!PasswordHasher.Verify(password, user.PasswordHash))
                return AuthResult.Fail("Parolă incorectă.");

            var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email),
        };
            claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var props = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                AllowRefresh = true,
            };

            var ctx = _http.HttpContext;
            if (ctx is null)
                return AuthResult.Fail("Context HTTP indisponibil (verifică configurarea).");

            await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
            _authState.Notify();

            return AuthResult.Ok();
        }

        public async Task SignOutAsync()
        {
            var ctx = _http.HttpContext;
            if (ctx is null) return;

            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _authState.Notify();
        }
        public async Task<AuthResult> SignUpAsync(string firstName, string lastName, string email, string password, string role)
        {
            // validare rol strict
            if (role != "Student" && role != "Teacher")
                return AuthResult.Fail("Rol invalid.");

            // email unic
            if (await _users.EmailExistsAsync(email))
                return AuthResult.Fail("Există deja un cont cu acest email.");

            // creare user
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Email = email.Trim(),
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                PasswordHash = PasswordHasher.Hash(password),
                Roles = new[] { role }
            };

            await _users.CreateAsync(user);

            // autologin după înregistrare
            return await SignInAsync(email, password, rememberMe: true);
        }
    }
}
