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
    }
}
