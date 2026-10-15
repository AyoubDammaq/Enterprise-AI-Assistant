namespace EnterpriseAIAssistant.Infrastructure.Memory
{
    /// <summary>
    /// Fournit l'identifiant utilisateur courant, défini par l'application
    /// (jamais par le LLM) et isolé par flux d'exécution asynchrone.
    /// </summary>
    public sealed class MemoryUserContext
    {
        private static readonly AsyncLocal<string?> _current = new();

        public string? CurrentUserId => _current.Value;

        public IDisposable BeginScope(string userId)
        {
            _current.Value = userId;
            return new Scope();
        }

        private sealed class Scope : IDisposable
        {
            public void Dispose() => _current.Value = null;
        }
    }
}
