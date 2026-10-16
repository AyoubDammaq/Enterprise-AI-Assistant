using EnterpriseAIAssistant.Infrastructure.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Plugins
{
    public class MemoryPluginTests
    {
        private static MemoryPlugin CreatePlugin(
            InMemoryMemoryService service,
            MemoryUserContext userContext) =>
            new(service, userContext, NullLogger<MemoryPlugin>.Instance);

        [Fact]
        public async Task SaveThenGet_ShouldReturnSavedMemory()
        {
            var service = new InMemoryMemoryService();
            var userContext = new MemoryUserContext();
            var plugin = CreatePlugin(service, userContext);

            using (userContext.BeginScope("demo-user-1"))
            {
                await plugin.SaveMemory("preferred_language", "French");

                var result = await plugin.GetMemory("preferred_language");

                Assert.Equal("French", result);
            }
        }

        [Fact]
        public async Task GetMemory_ShouldNotReturnAnotherUsersMemory()
        {
            var service = new InMemoryMemoryService();
            var userContext = new MemoryUserContext();
            var plugin = CreatePlugin(service, userContext);

            using (userContext.BeginScope("demo-user-1"))
            {
                await plugin.SaveMemory("preferred_language", "French");
            }

            using (userContext.BeginScope("demo-user-2"))
            {
                var result = await plugin.GetMemory("preferred_language");

                Assert.Contains("No memory found", result);
            }
        }

        [Fact]
        public async Task GetMemory_WithoutUserContext_ShouldThrow()
        {
            var service = new InMemoryMemoryService();
            var userContext = new MemoryUserContext();
            var plugin = CreatePlugin(service, userContext);

            // Aucun scope ouvert : l'application n'a pas établi d'identité.
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => plugin.GetMemory("preferred_language"));
        }

        // Scénario : DeleteMemory -> la clé est supprimée.
        [Fact]
        public async Task DeleteMemory_ShouldRemoveKey()
        {
            var service = new InMemoryMemoryService();
            var userContext = new MemoryUserContext();
            var plugin = CreatePlugin(service, userContext);

            using (userContext.BeginScope("demo-user-1"))
            {
                await plugin.SaveMemory("preferred_language", "French");

                await plugin.DeleteMemory("preferred_language");

                var result = await plugin.GetMemory("preferred_language");

                Assert.Contains("No memory found", result);
            }
        }

        // Scénario : paramètre invalide (clé vide) -> l'opération est rejetée.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SaveMemory_WithInvalidKey_ShouldThrow(string invalidKey)
        {
            var service = new InMemoryMemoryService();
            var userContext = new MemoryUserContext();
            var plugin = CreatePlugin(service, userContext);

            using (userContext.BeginScope("demo-user-1"))
            {
                await Assert.ThrowsAsync<ArgumentException>(
                    () => plugin.SaveMemory(invalidKey, "French"));
            }
        }

        // Scénario : paramètre invalide (valeur vide) -> l'opération est rejetée.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SaveMemory_WithInvalidValue_ShouldThrow(string invalidValue)
        {
            var service = new InMemoryMemoryService();
            var userContext = new MemoryUserContext();
            var plugin = CreatePlugin(service, userContext);

            using (userContext.BeginScope("demo-user-1"))
            {
                await Assert.ThrowsAsync<ArgumentException>(
                    () => plugin.SaveMemory("preferred_language", invalidValue));
            }
        }

        // Scénario : Utilisateur A puis B -> aucune fuite entre utilisateurs.
        // Chaque utilisateur récupère uniquement sa propre valeur pour une
        // clé identique.
        [Fact]
        public async Task SaveAndGet_ForTwoUsers_ShouldNotLeakBetweenThem()
        {
            var service = new InMemoryMemoryService();
            var userContext = new MemoryUserContext();
            var plugin = CreatePlugin(service, userContext);

            using (userContext.BeginScope("user-A"))
            {
                await plugin.SaveMemory("favorite_football_team", "FC Barcelona");
            }

            using (userContext.BeginScope("user-B"))
            {
                await plugin.SaveMemory("favorite_football_team", "Real Madrid");
            }

            using (userContext.BeginScope("user-A"))
            {
                Assert.Equal(
                    "FC Barcelona",
                    await plugin.GetMemory("favorite_football_team"));
            }

            using (userContext.BeginScope("user-B"))
            {
                Assert.Equal(
                    "Real Madrid",
                    await plugin.GetMemory("favorite_football_team"));
            }
        }
    }
}
