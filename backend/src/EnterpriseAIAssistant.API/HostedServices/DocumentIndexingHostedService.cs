using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Infrastructure.Documents;
using Microsoft.Extensions.Options;

namespace EnterpriseAIAssistant.API.HostedServices
{
    public sealed class DocumentIndexingHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<DocumentLoadingOptions> options,
        IHostEnvironment environment,
        ILogger<DocumentIndexingHostedService> logger) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var configuredPath = options.Value.RootDirectory;

            var documentsPath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(environment.ContentRootPath, configuredPath);

            // Create the directory if it does not exist.
            Directory.CreateDirectory(documentsPath);

            logger.LogInformation(
                "Starting document indexing from {DocumentsPath}",
                documentsPath);

            using var scope = scopeFactory.CreateScope();

            var documentLoader = scope.ServiceProvider
                .GetRequiredService<IDocumentLoader>();

            var knowledgeIndexer = scope.ServiceProvider
                .GetRequiredService<IKnowledgeIndexer>();

            var chunks = await documentLoader.LoadAsync(
                documentsPath,
                cancellationToken);

            if (chunks.Count == 0)
            {
                logger.LogWarning(
                    "No supported documents found in {DocumentsPath}",
                    documentsPath);

                return;
            }

            var indexedCount = await knowledgeIndexer.IndexAsync(
                chunks,
                cancellationToken);

            logger.LogInformation(
                "Document indexing completed. {ChunkCount} chunks indexed.",
                indexedCount);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}