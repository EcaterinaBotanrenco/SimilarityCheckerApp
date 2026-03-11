using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SimilarityChecker.UI.Authentication
{
    public sealed class ApiAuthorizationMessageHandler : DelegatingHandler
    {
        private readonly AuthSessionStore _sessionStore;

        public ApiAuthorizationMessageHandler(AuthSessionStore sessionStore)
        {
            _sessionStore = sessionStore;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = _sessionStore.Session?.Token;

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}