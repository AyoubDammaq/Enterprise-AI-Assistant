using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Infrastructure.AI;
using EnterpriseAIAssistant.Infrastructure.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseAIAssistant.Infrastructure.Extensions
{
    public static class DependencieInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // Register your services here
            services.Scan(scan => scan
                .FromAssemblyOf<SemanticKernelChatService>()
                .AddClasses()
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.AddSingleton<IConversationStore, InMemoryConversationStore>();
            services.AddSingleton<IMemoryService, InMemoryMemoryService>();

            services.AddSingleton<MemoryPlugin>();
            services.AddSingleton<MemoryUserContext>();

            return services;
        }
    }
}
