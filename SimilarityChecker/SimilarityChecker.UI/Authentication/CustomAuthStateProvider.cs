using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SimilarityChecker.UI.Authentication
{
    public sealed class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _http;

        public CustomAuthStateProvider(IHttpContextAccessor http)
        {
            _http = http;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var user = _http.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
            return Task.FromResult(new AuthenticationState(user));
        }

        public void Notify() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
