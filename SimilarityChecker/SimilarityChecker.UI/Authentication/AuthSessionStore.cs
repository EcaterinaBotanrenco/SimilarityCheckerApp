namespace SimilarityChecker.UI.Authentication
{
    public sealed class AuthSessionStore
    {
        public AuthSessionModel? Session { get; private set; }

        public void SetSession(AuthSessionModel session)
        {
            Session = session;
        }

        public void Clear()
        {
            Session = null;
        }
    }
}
