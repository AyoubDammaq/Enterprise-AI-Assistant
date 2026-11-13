using EnterpriseAIAssistant.Application.DTOs;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Documents
{
    public class DocumentChunkTests
    {
        [Fact]
        public void ChunkId_ShouldBeDeterministic()
        {
            // Arrange
            var first = new DocumentChunk(
                "support-faq",
                "documents/support-faq.md",
                0,
                "First chunk");

            var second = new DocumentChunk(
                "support-faq",
                "documents/support-faq.md",
                0,
                "Different content");

            // Assert
            Assert.Equal(
                "support-faq::chunk-0000",
                first.ChunkId);

            Assert.Equal(first.ChunkId, second.ChunkId);
        }

        [Fact]
        public void ChunkId_ShouldBeDifferentForDifferentChunkIndexes()
        {
            // Arrange
            var first = new DocumentChunk(
                "support-faq",
                "documents/support-faq.md",
                0,
                "First chunk");

            var second = new DocumentChunk(
                "support-faq",
                "documents/support-faq.md",
                1,
                "Second chunk");

            // Assert
            Assert.NotEqual(first.ChunkId, second.ChunkId);
        }

        [Fact]
        public void ChunkId_ShouldBeDifferentForDifferentDocuments()
        {
            // Arrange
            var first = new DocumentChunk(
                "support-faq",
                "documents/support-faq.md",
                0,
                "Content");

            var second = new DocumentChunk(
                "technical-guide",
                "documents/technical-guide.md",
                0,
                "Content");

            // Assert
            Assert.NotEqual(first.ChunkId, second.ChunkId);
        }

        [Fact]
        public void Section_ShouldBeNullByDefault()
        {
            // Arrange
            var chunk = new DocumentChunk(
                "support-faq",
                "documents/support-faq.md",
                0,
                "Content");

            // Assert
            Assert.Null(chunk.Section);
        }

        [Fact]
        public void Section_ShouldBeSettable()
        {
            // Arrange
            var chunk = new DocumentChunk(
                "support-faq",
                "documents/support-faq.md",
                0,
                "Content")
            {
                Section = "Account Management"
            };

            // Assert
            Assert.Equal("Account Management", chunk.Section);
        }

        [Fact]
        public void ExistingConstructor_ShouldPreserveAllProperties()
        {
            // Arrange
            var chunk = new DocumentChunk(
                "support-faq",
                "documents/support-faq.md",
                2,
                "How to reset your password?");

            // Assert
            Assert.Equal("support-faq", chunk.DocumentId);
            Assert.Equal("documents/support-faq.md", chunk.Source);
            Assert.Equal(2, chunk.ChunkIndex);
            Assert.Equal(
                "How to reset your password?",
                chunk.Content);
        }

        [Fact]
        public void ChunkId_ShouldFormatLargeIndexesCorrectly()
        {
            var chunk = new DocumentChunk(
                "support-faq",
                "documents/support-faq.md",
                12345,
                "Content");

            Assert.Equal(
                "support-faq::chunk-12345",
                chunk.ChunkId);
        }
    }
}
