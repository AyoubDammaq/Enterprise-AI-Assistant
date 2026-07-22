using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EnterpriseAIAssistant.API.HealthChecks
{
    public static class ApiHealthCheck
    {
        public static void ConfigureHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            var hcBuilder = services.AddHealthChecks();

            // Add SQL Server health check only if connection string is provided
            var feedbackConn = configuration.GetConnectionString("Feedback");
            if (!string.IsNullOrWhiteSpace(feedbackConn))
            {
                hcBuilder.AddSqlServer(feedbackConn, healthQuery: "select 1", name: "SQL Server", failureStatus: HealthStatus.Unhealthy, tags: new[] { "Feedback", "Database" });
            }

            hcBuilder
                .AddCheck<RemoteHealthCheck>("Remote endpoints Health Check", failureStatus: HealthStatus.Unhealthy)
                .AddCheck<MemoryHealthCheck>("Feedback Service Memory Check", failureStatus: HealthStatus.Unhealthy, tags: new[] { "Feedback Service" })
                .AddUrlGroup(new Uri("https://localhost:5035/api/v1/heartbeats/ping"), name: "base URL", failureStatus: HealthStatus.Unhealthy);

            //services.AddHealthChecksUI();
            services.AddHealthChecksUI(opt =>
            {
                opt.SetEvaluationTimeInSeconds(10); //time in seconds between check    
                opt.MaximumHistoryEntriesPerEndpoint(60); //maximum history of checks    
                opt.SetApiMaxActiveRequests(1); //api requests concurrency    
                opt.AddHealthCheckEndpoint("feedback api", "/api/health"); //map health check api    

            })
                .AddInMemoryStorage();
        }
    }
}
