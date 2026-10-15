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
    }
}
