using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Infrastructure.AI;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseAIAssistant.Infrastructure.Extensions
{
    public static class DependencieInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // Register your services here
            services.AddScoped<IAIChatService, SemanticKernelChatService>();

            return services;
        }
    }
}
