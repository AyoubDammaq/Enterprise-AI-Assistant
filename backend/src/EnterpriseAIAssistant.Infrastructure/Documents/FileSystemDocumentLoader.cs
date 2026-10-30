using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace EnterpriseAIAssistant.Infrastructure.Documents;

public sealed class FileSystemDocumentLoader : IDocumentLoader
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".txt",
            ".md"
        };

    private readonly DocumentLoadingOptions _options;
    private readonly TextChunker _chunker;

    public FileSystemDocumentLoader(
        IOptions<DocumentLoadingOptions> options)
    {
        _options = options.Value;

        ValidateOptions(_options);

        _chunker = new TextChunker(
            _options.ChunkSize,
            _options.ChunkOverlap);
    }

    public async Task<IReadOnlyList<DocumentChunk>> LoadAsync(
        string? path = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var requestedPath = path ?? _options.RootDirectory;

        ArgumentException.ThrowIfNullOrWhiteSpace(requestedPath);

        var fullPath = Path.GetFullPath(requestedPath);

        if (File.Exists(fullPath))
        {
            EnsureSupportedExtension(fullPath);

            return await LoadFileAsync(
                fullPath,
                Path.GetDirectoryName(fullPath)!,
                cancellationToken);
        }

        if (Directory.Exists(fullPath))
        {
            return await LoadDirectoryAsync(
                fullPath,
                cancellationToken);
        }

        throw new FileNotFoundException(
            $"The specified file or directory was not found: '{fullPath}'.",
            fullPath);
    }


    private async Task<IReadOnlyList<DocumentChunk>> LoadDirectoryAsync(
        string directoryPath,
        CancellationToken cancellationToken)
    {
        var chunks = new List<DocumentChunk>();

        var files = Directory
            .EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
            .Where(file => SupportedExtensions.Contains(Path.GetExtension(file)))
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileChunks = await LoadFileAsync(
                file,
                directoryPath,
                cancellationToken);

            chunks.AddRange(fileChunks);
        }

        return chunks;
    }



    private async Task<IReadOnlyList<DocumentChunk>> LoadFileAsync(
        string filePath,
        string rootDirectory,
        CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(
            filePath,
            cancellationToken);

        var source = Path.GetRelativePath(rootDirectory, filePath);
        var documentId = source;

        return _chunker.Chunk(
            documentId,
            source,
            content);
    }


    private static void EnsureSupportedExtension(string filePath)
    {
        var extension = Path.GetExtension(filePath);

        if (!SupportedExtensions.Contains(extension))
        {
            throw new NotSupportedException(
                $"File extension '{extension}' is not supported. " +
                "Supported extensions are .txt and .md.");
        }
    }

    private static void ValidateOptions(DocumentLoadingOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.RootDirectory);

        if (options.ChunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.ChunkSize),
                "Chunk size must be greater than zero.");
        }

        if (options.ChunkOverlap < 0 ||
            options.ChunkOverlap >= options.ChunkSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.ChunkOverlap),
                "Chunk overlap must be non-negative and smaller than chunk size.");
        }
    }
}
