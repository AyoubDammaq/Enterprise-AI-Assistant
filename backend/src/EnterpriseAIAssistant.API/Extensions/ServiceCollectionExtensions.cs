using EnterpriseAIAssistant.Application.Extensions;
using EnterpriseAIAssistant.Infrastructure.Extensions;
namespace EnterpriseAIAssistant.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register application layer services
            services.AddApplication(configuration);

            // Register infrastructure layer services
            services.AddInfrastructure();

            return services;
        }
    }
}
