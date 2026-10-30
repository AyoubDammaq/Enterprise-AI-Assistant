using EnterpriseAIAssistant.Application.Auth;
using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Infrastructure.AI;
using EnterpriseAIAssistant.Infrastructure.Documents;
using EnterpriseAIAssistant.Infrastructure.Identity;
using EnterpriseAIAssistant.Infrastructure.Memory;
using EnterpriseAIAssistant.Infrastructure.Retrieval;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseAIAssistant.Infrastructure.Extensions
{
    public static class DependencieInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Keep retrieval registrations out of the generic Scrutor scan so their lifetimes are explicit.
            services.Scan(scan => scan
                .FromAssemblyOf<SemanticKernelChatService>()
                .AddClasses(classes => classes.Where(type =>
                    type.Namespace != typeof(InMemoryVectorStore).Namespace))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.AddDbContext<AuthDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("AuthDatabase")));

            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AuthDbContext>();

            services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
            services.AddScoped<JwtTokenService>();
            services.AddScoped<IAuthService, AuthService>();

            // Existing AI and memory services — preserve these registrations.
            services.AddSingleton<IConversationStore, InMemoryConversationStore>();
            services.AddSingleton<IMemoryService, InMemoryMemoryService>();
            services.AddSingleton<MemoryPlugin>();
            services.AddSingleton<MemoryUserContext>();

            services.Configure<DocumentLoadingOptions>(
                configuration.GetSection(DocumentLoadingOptions.SectionName));

            // RAG Day 5: Ollama embeddings and in-memory vector retrieval.
            services.Configure<OllamaEmbeddingOptions>(
                configuration.GetSection(OllamaEmbeddingOptions.SectionName));
            services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>();
            services.AddSingleton<IVectorStore, InMemoryVectorStore>();
            services.AddScoped<IKnowledgeIndexer, KnowledgeIndexer>();
            services.AddScoped<IKnowledgeRetriever, KnowledgeRetriever>();

            return services;
        }
    }
}
