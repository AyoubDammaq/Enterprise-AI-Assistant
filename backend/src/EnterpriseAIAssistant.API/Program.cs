using EnterpriseAIAssistant.API.HealthChecks;
using EnterpriseAIAssistant.API.Extensions;
using EnterpriseAIAssistant.Application.Extensions;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Add Gemini configuration 
var modelId = builder.Configuration["Gemini:ModelId"] 
    ?? throw new InvalidOperationException(
        "Gemini:ModelId is not configured.");

var apiKey = builder.Configuration["Gemini:ApiKey"] 
    ?? throw new InvalidOperationException(
        "Gemini:ApiKey is not configured.");

// Add Ollama configuration
var ollamaEndpoint =
    builder.Configuration["Ollama:Endpoint"]
    ?? throw new InvalidOperationException(
        "Ollama endpoint is not configured.");

var ollamaModel =
    builder.Configuration["Ollama:Model"]
    ?? throw new InvalidOperationException(
        "Ollama model is not configured.");

builder.Services.AddSingleton<Kernel>(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();

    //kernelBuilder.AddGoogleAIGeminiChatCompletion(
    //    modelId: modelId,
    //    apiKey: apiKey);

    kernelBuilder.AddOllamaChatCompletion(
        modelId: ollamaModel,
        endpoint: new Uri(ollamaEndpoint),
        serviceId: "ollama");

    return kernelBuilder.Build();
});

builder.Services.AddServices(builder.Configuration); // Register application services
builder.Services.AddApplication(builder.Configuration); // Register application services

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

//Congiguring Health Ckeck
builder.Services.ConfigureHealthChecks(builder.Configuration);

var app = builder.Build();

//HealthCheck Middleware
app.MapHealthChecks("/api/health", new HealthCheckOptions()
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.UseHealthChecksUI(options =>
{
    options.UIPath = "/healthcheck-ui";
    //options.AddCustomStylesheet("./HealthCheck/Custom.css");

});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
