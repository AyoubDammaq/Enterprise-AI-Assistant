using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseAIAssistant.Application.Extensions
{
    public static class DependencieInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            // Register your application services here using configuration if needed
            return services;
        }
    }
}
