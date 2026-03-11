using Microsoft.AspNetCore.Components.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SimilarityChecker.UI.Authentication
{
    public sealed class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly AuthSessionStore _sessionStore;

        public CustomAuthStateProvider(AuthSessionStore sessionStore)
        {
            _sessionStore = sessionStore;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var session = _sessionStore.Session;

            if (session is null || string.IsNullOrWhiteSpace(session.Token))
            {
                var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                return Task.FromResult(new AuthenticationState(anonymous));
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, session.UserId),
                new(ClaimTypes.Name, session.DisplayName),
                new(ClaimTypes.Email, session.Email),
                new("access_token", session.Token)
            };

            claims.AddRange(session.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var identity = new ClaimsIdentity(claims, "JwtAuth");
            var user = new ClaimsPrincipal(identity);

            return Task.FromResult(new AuthenticationState(user));
        }

        public void NotifyUserAuthentication()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public void NotifyUserLogout()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}