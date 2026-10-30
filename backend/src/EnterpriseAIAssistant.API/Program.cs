using EnterpriseAIAssistant.API.Extensions;
using EnterpriseAIAssistant.Infrastructure.AI.Plugins;
using EnterpriseAIAssistant.Infrastructure.Identity;
using EnterpriseAIAssistant.Infrastructure.Memory;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.SemanticKernel;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);

var issuer = jwtSection["Issuer"];
var audience = jwtSection["Audience"];
var secret = jwtSection["SecretKey"];

if (string.IsNullOrWhiteSpace(issuer) ||
    string.IsNullOrWhiteSpace(audience) ||
    string.IsNullOrWhiteSpace(secret))
{
    throw new InvalidOperationException(
        "La configuration JWT est incomplète.");
}

byte[] keyBytes;

try
{
    keyBytes = Convert.FromBase64String(secret);
}
catch (FormatException ex)
{
    throw new InvalidOperationException(
        "Jwt:SecretKey doit être une clé Base64 valide.", ex);
}

if (keyBytes.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:SecretKey doit contenir au moins 32 octets.");
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateAudience = true,
                ValidAudience = audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,

                ClockSkew = TimeSpan.FromSeconds(30),

                NameClaimType = JwtRegisteredClaimNames.Sub,
                RoleClaimType = "role"
            };
    });

builder.Services.AddAuthorization();


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

    kernelBuilder.Plugins.AddFromType<DateTimePlugin>("DateTimePlugin");
    kernelBuilder.Plugins.AddFromType<CalculatorPlugin>("CalculatorPlugin");

    var kernel = kernelBuilder.Build();

    // Réutilise MemoryPlugin et IMemoryService enregistrés par DI.
    var memoryPlugin = sp.GetRequiredService<MemoryPlugin>();
    kernel.Plugins.AddFromObject(memoryPlugin, "MemoryPlugin");

    foreach (var plugin in kernel.Plugins)
    {
        Console.WriteLine($"PLUGIN: {plugin.Name}");

        foreach (var function in plugin)
        {
            Console.WriteLine(
                $"  FUNCTION: {function.Name}");
        }
    }

    return kernel;
});

builder.Services.AddServices(builder.Configuration); // Register application services

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT access token."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});
builder.Services.AddHealthChecks();

//Congiguring Health Ckeck
//builder.Services.ConfigureHealthChecks(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
