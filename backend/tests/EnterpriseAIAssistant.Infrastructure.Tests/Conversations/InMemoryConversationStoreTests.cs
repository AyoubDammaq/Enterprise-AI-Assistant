using EnterpriseAIAssistant.Infrastructure.Memory;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Conversations
{
    public class InMemoryConversationStoreTests
    {
        private readonly InMemoryConversationStore _store = new();

        private readonly Guid _userA = Guid.NewGuid();
        private readonly Guid _userB = Guid.NewGuid();

        [Fact]
        public void GetOrCreate_SameOwner_ReturnsSameHistory()
        {
            var first = _store.GetOrCreate("conv-1", _userA);
            var second = _store.GetOrCreate("conv-1", _userA);

            Assert.Same(first, second);
        }

        [Fact]
        public void GetOrCreate_DifferentConversation_ReturnsDifferentHistory()
        {
            var historyA = _store.GetOrCreate("conv-A", _userA);
            var historyB = _store.GetOrCreate("conv-B", _userA);

            Assert.NotSame(historyA, historyB);
        }

        [Fact]
        public void GetOrCreate_DifferentOwner_ThrowsUnauthorizedAccessException()
        {
            var history = _store.GetOrCreate("conv-1", _userA);
            history.AddUserMessage("Private message");

            Assert.Throws<UnauthorizedAccessException>(
                () => _store.GetOrCreate("conv-1", _userB));
        }

        [Fact]
        public void TryGet_Owner_ReturnsHistory()
        {
            var expected = _store.GetOrCreate("conv-1", _userA);
            expected.AddUserMessage("Hello");

            var found = _store.TryGet(
                "conv-1", _userA, out var actual);

            Assert.True(found);
            Assert.Same(expected, actual);
            Assert.Contains(actual!, m => m.Content == "Hello");
        }

        [Fact]
        public void TryGet_DifferentOwner_ReturnsFalseAndNull()
        {
            _store.GetOrCreate("conv-1", _userA)
                .AddUserMessage("Private message");

            var found = _store.TryGet(
                "conv-1", _userB, out var history);

            Assert.False(found);
            Assert.Null(history);
        }

        [Fact]
        public void TryGet_UnknownConversation_ReturnsFalseAndNull()
        {
            var found = _store.TryGet(
                "unknown", _userA, out var history);

            Assert.False(found);
            Assert.Null(history);
        }

        [Fact]
        public void Delete_Owner_CanDeleteConversation()
        {
            _store.GetOrCreate("conv-1", _userA);

            Assert.True(_store.Delete("conv-1", _userA));
            Assert.False(_store.TryGet(
                "conv-1", _userA, out _));
        }

        [Fact]
        public void Delete_DifferentOwner_CannotDeleteConversation()
        {
            var original = _store.GetOrCreate("conv-1", _userA);
            original.AddUserMessage("Keep this");

            Assert.False(_store.Delete("conv-1", _userB));

            Assert.True(_store.TryGet(
                "conv-1", _userA, out var remaining));

            Assert.Same(original, remaining);
            Assert.Contains(remaining!, m => m.Content == "Keep this");
        }
    }
}