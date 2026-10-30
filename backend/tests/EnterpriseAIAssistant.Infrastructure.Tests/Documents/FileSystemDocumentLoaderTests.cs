
using EnterpriseAIAssistant.Infrastructure.Documents;
using Microsoft.Extensions.Options;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Documents;

public sealed class FileSystemDocumentLoaderTests : IDisposable
{
    private readonly string _testDirectory =
        Path.Combine(Path.GetTempPath(), $"DocumentLoaderTests_{Guid.NewGuid():N}");

    private readonly FileSystemDocumentLoader _loader;

    public FileSystemDocumentLoaderTests()
    {
        Directory.CreateDirectory(_testDirectory);

        var options = Options.Create(new DocumentLoadingOptions
        {
            RootDirectory = _testDirectory,
            ChunkSize = 20,
            ChunkOverlap = 5
        });

        _loader = new FileSystemDocumentLoader(options);
    }

    [Fact]
    public async Task LoadAsync_NoPath_UsesConfiguredRootDirectory()
    {
        // Arrange
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "configured.txt"),
            "Configured root");

        // Act
        var result = await _loader.LoadAsync();

        // Assert
        var chunk = Assert.Single(result);
        Assert.Equal("configured.txt", chunk.Source);
        Assert.Equal("configured.txt", chunk.DocumentId);
        Assert.Equal("Configured root", chunk.Content);
    }

    [Fact]
    public async Task LoadAsync_SingleTextFile_ReturnsChunksWithMetadata()
    {
        var filePath = Path.Combine(_testDirectory, "sample.txt");
        await File.WriteAllTextAsync(filePath, "Hello document");

        var result = await _loader.LoadAsync(filePath);

        var chunk = Assert.Single(result);
        Assert.Equal("sample.txt", chunk.DocumentId);
        Assert.Equal("sample.txt", chunk.Source);
        Assert.Equal(0, chunk.ChunkIndex);
        Assert.Equal("Hello document", chunk.Content);
    }

    [Fact]
    public async Task LoadAsync_MarkdownFile_LoadsContent()
    {
        var filePath = Path.Combine(_testDirectory, "guide.md");
        await File.WriteAllTextAsync(filePath, "# Guide\n\nSome content");

        var result = await _loader.LoadAsync(filePath);

        Assert.NotEmpty(result);
        Assert.Contains(result, chunk => chunk.Content.Contains("# Guide"));
    }

    [Fact]
    public async Task LoadAsync_EmptyFile_ReturnsNoChunks()
    {
        var filePath = Path.Combine(_testDirectory, "empty.txt");
        await File.WriteAllTextAsync(filePath, "");

        var result = await _loader.LoadAsync(filePath);

        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadAsync_UnsupportedFileExtension_Throws()
    {
        var filePath = Path.Combine(_testDirectory, "data.json");
        await File.WriteAllTextAsync(filePath, "{}");

        await Assert.ThrowsAsync<NotSupportedException>(
            () => _loader.LoadAsync(filePath));
    }

    [Fact]
    public async Task LoadAsync_Directory_LoadsSupportedFilesOnly()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "one.txt"), "Text one");

        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "two.md"), "Text two");

        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "ignored.json"), "{}");

        var result = await _loader.LoadAsync(_testDirectory);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, chunk => chunk.Source == "one.txt");
        Assert.Contains(result, chunk => chunk.Source == "two.md");
        Assert.DoesNotContain(result, chunk => chunk.Source.EndsWith(".json"));
    }

    [Fact]
    public async Task LoadAsync_NestedFiles_UsesRelativePathsAsSources()
    {
        var nestedDirectory = Path.Combine(_testDirectory, "guides");
        Directory.CreateDirectory(nestedDirectory);

        await File.WriteAllTextAsync(
            Path.Combine(nestedDirectory, "setup.md"), "Setup instructions");

        var result = await _loader.LoadAsync(_testDirectory);

        var chunk = Assert.Single(result);
        Assert.Equal(Path.Combine("guides", "setup.md"), chunk.Source);
        Assert.Equal(chunk.Source, chunk.DocumentId);
    }

    [Fact]
    public async Task LoadAsync_MissingPath_ThrowsFileNotFoundException()
    {
        var missingPath = Path.Combine(_testDirectory, "missing.txt");

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _loader.LoadAsync(missingPath));
    }

    [Fact]
    public async Task LoadAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _loader.LoadAsync(_testDirectory, cancellationSource.Token));
    }

    [Fact]
    public async Task LoadAsync_UppercaseExtension_LoadsFile()
    {
        var filePath = Path.Combine(_testDirectory, "GUIDE.MD");
        await File.WriteAllTextAsync(filePath, "Markdown content");

        var result = await _loader.LoadAsync(filePath);

        var chunk = Assert.Single(result);
        Assert.Equal("GUIDE.MD", chunk.Source);
        Assert.Equal("Markdown content", chunk.Content);
    }

    [Fact]
    public async Task LoadAsync_Directory_ReturnsFilesInDeterministicOrder()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "c.txt"), "Third");

        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "a.txt"), "First");

        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "b.md"), "Second");

        var result = await _loader.LoadAsync(_testDirectory);

        Assert.Equal(
            new[] { "a.txt", "b.md", "c.txt" },
            result.Select(chunk => chunk.Source));
    }

    [Fact]
    public async Task LoadAsync_NullPath_UsesConfiguredRootDirectory()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, "default.txt"),
            "Default path");

        var result = await _loader.LoadAsync(null);

        var chunk = Assert.Single(result);
        Assert.Equal("default.txt", chunk.Source);
        Assert.Equal("Default path", chunk.Content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}
