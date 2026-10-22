
using EnterpriseAIAssistant.Application.Auth;
using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Infrastructure.AI;
using EnterpriseAIAssistant.Infrastructure.Identity;
using EnterpriseAIAssistant.Infrastructure.Memory;
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
            // Existing application services
            services.Scan(scan => scan
                .FromAssemblyOf<SemanticKernelChatService>()
                .AddClasses()
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            // PostgreSQL
            services.AddDbContext<AuthDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("AuthDatabase")));

            // ASP.NET Core Identity
            services.AddIdentityCore<ApplicationUser>(options =>
            {
                // Politique de mot de passe
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;

                // Protection contre les tentatives répétées
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan =
                    TimeSpan.FromMinutes(15);

                // Email
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AuthDbContext>();

            // Bind JWT configuration for services using IOptions<JwtOptions>
            services.Configure<JwtOptions>(
                configuration.GetSection(JwtOptions.SectionName));

            // Authentication services
            services.AddScoped<JwtTokenService>();
            services.AddScoped<IAuthService, AuthService>();

            // Existing AI and memory services — preserve these registrations
            services.AddSingleton<IConversationStore, InMemoryConversationStore>();
            services.AddSingleton<IMemoryService, InMemoryMemoryService>();
            services.AddSingleton<MemoryPlugin>();
            services.AddSingleton<MemoryUserContext>();

            return services;
        }
    }
}
