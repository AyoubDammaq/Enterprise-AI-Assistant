using EnterpriseAIAssistant.Infrastructure.Memory;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Memory
{
    public class InMemoryMemoryServiceTests
    {
        [Fact]
        public async Task SaveAndGet_ShouldReturnSavedValue()
        {
            var memory = new InMemoryMemoryService();

            await memory.SaveAsync(
                "user-001",
                "preferred_language",
                "French");

            var result = await memory.GetAsync(
                "user-001",
                "preferred_language");

            Assert.Equal("French", result);
        }

        [Fact]
        public async Task Get_ShouldIsolateUsers()
        {
            var memory = new InMemoryMemoryService();

            await memory.SaveAsync(
                "user-001", "preferred_language", "French");

            await memory.SaveAsync(
                "user-002", "preferred_language", "English");

            Assert.Equal(
                "French",
                await memory.GetAsync("user-001", "preferred_language"));

            Assert.Equal(
                "English",
                await memory.GetAsync("user-002", "preferred_language"));
        }

        [Fact]
        public async Task Delete_ShouldRemoveValue()
        {
            var memory = new InMemoryMemoryService();

            await memory.SaveAsync(
                "user-001", "preferred_language", "French");

            await memory.DeleteAsync(
                "user-001", "preferred_language");

            var result = await memory.GetAsync(
                "user-001", "preferred_language");

            Assert.Null(result);
        }

        // ---------------------------------------------------------------
        // Partie B — Long-Term Memory (InMemoryMemoryService isolé :
        // sans ChatHistory, sans MemoryPlugin, sans LLM).
        // ---------------------------------------------------------------

        // Test 1 — Save puis Get
        [Fact]
        public async Task GetMemory_AfterSave_ShouldReturnSavedValue()
        {
            var service = new InMemoryMemoryService();

            await service.SaveAsync("user-A", "preferred_language", "French");

            var result = await service.GetAsync("user-A", "preferred_language");

            Assert.Equal("French", result);
        }

        // Test 2 — Deux utilisateurs ne partagent pas leur mémoire,
        // même avec une clé identique (isolation via le préfixe userId).
        [Fact]
        public async Task Memory_ShouldBeIsolatedBetweenUsers()
        {
            var service = new InMemoryMemoryService();

            await service.SaveAsync("user-A", "preferred_language", "French");
            await service.SaveAsync("user-B", "preferred_language", "English");

            var resultA = await service.GetAsync("user-A", "preferred_language");
            var resultB = await service.GetAsync("user-B", "preferred_language");

            Assert.Equal("French", resultA);
            Assert.Equal("English", resultB);
        }

        // Test 3 — Delete ne supprime que la clé ciblée.
        [Fact]
        public async Task DeleteMemory_ShouldRemoveOnlyTargetMemory()
        {
            var service = new InMemoryMemoryService();

            await service.SaveAsync("user-A", "preferred_language", "French");
            await service.SaveAsync("user-A", "preferred_framework", ".NET");

            await service.DeleteAsync("user-A", "preferred_language");

            var deleted = await service.GetAsync("user-A", "preferred_language");
            var retained = await service.GetAsync("user-A", "preferred_framework");

            Assert.Null(deleted);
            Assert.Equal(".NET", retained);
        }

        // Test 4 — Mémoire inconnue : comportement documenté = retourne null,
        // sans créer accidentellement d'entrée (lire ne doit pas « matérialiser »
        // la clé). On le vérifie en boîte noire : lectures successives + une
        // sauvegarde ultérieure reste bien la première valeur écrite.
        [Fact]
        public async Task GetMemory_ForUnknownKey_ShouldReturnNull_AndNotCreateEntry()
        {
            var service = new InMemoryMemoryService();

            var firstRead = await service.GetAsync("user-A", "unknown_key");
            var secondRead = await service.GetAsync("user-A", "unknown_key");

            Assert.Null(firstRead);
            Assert.Null(secondRead);

            // La lecture n'a pas créé d'entrée : un Save ultérieur fait foi.
            await service.SaveAsync("user-A", "unknown_key", "now_set");
            Assert.Equal("now_set", await service.GetAsync("user-A", "unknown_key"));
        }

        // Test 5 — Persistance réelle (LIMITE CONNUE).
        // InMemoryMemoryService stocke dans un ConcurrentDictionary : les données
        // ne survivent qu'à la durée de vie du processus. Un Save -> Get ne prouve
        // donc PAS une persistance après redémarrage. Ce test est volontairement
        // ignoré jusqu'à l'adoption d'un stockage durable (ex. base de données).
        [Fact(Skip = "Limite connue : stockage volatil en mémoire, pas de persistance après redémarrage du processus. À implémenter avec un stockage durable.")]
        public Task Memory_ShouldPersistAcrossProcessRestarts()
        {
            // Nécessite un stockage durable (SQL, Redis, fichier, ...).
            return Task.CompletedTask;
        }
    }
}
