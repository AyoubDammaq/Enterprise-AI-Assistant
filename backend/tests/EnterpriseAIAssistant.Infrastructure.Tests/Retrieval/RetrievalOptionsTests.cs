using System.Globalization;
using EnterpriseAIAssistant.Infrastructure.Configuration;
using EnterpriseAIAssistant.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Retrieval;

public sealed class RetrievalOptionsTests
{
    [Fact]
    public void RetrievalOptions_DefaultValues_AreCorrect()
    {
        var options = new RetrievalOptions();

        Assert.Equal(5, options.TopK);
        Assert.Equal(0.50, options.MinimumSimilarityScore);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void AddInfrastructure_ValidTopK_BindsSuccessfully(int topK)
    {
        using var provider = CreateProvider(new Dictionary<string, string?>
        {
            ["Retrieval:TopK"] = topK.ToString(CultureInfo.InvariantCulture)
        });

        var options = provider.GetRequiredService<IOptions<RetrievalOptions>>().Value;

        Assert.Equal(topK, options.TopK);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void AddInfrastructure_ValidSimilarityScore_BindsSuccessfully(double score)
    {
        using var provider = CreateProvider(new Dictionary<string, string?>
        {
            ["Retrieval:MinimumSimilarityScore"] =
                score.ToString("R", CultureInfo.InvariantCulture)
        });

        var options = provider.GetRequiredService<IOptions<RetrievalOptions>>().Value;

        Assert.Equal(score, options.MinimumSimilarityScore);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void AddInfrastructure_InvalidTopK_ThrowsOptionsValidationException(int topK)
    {
        using var provider = CreateProvider(new Dictionary<string, string?>
        {
            ["Retrieval:TopK"] = topK.ToString(CultureInfo.InvariantCulture)
        });

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<RetrievalOptions>>().Value);
    }

    [Theory]
    [MemberData(nameof(InvalidSimilarityScores))]
    public void AddInfrastructure_InvalidSimilarityScore_ThrowsOptionsValidationException(
        double score)
    {
        using var provider = CreateProvider(new Dictionary<string, string?>
        {
            ["Retrieval:MinimumSimilarityScore"] =
                score.ToString("R", CultureInfo.InvariantCulture)
        });

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<RetrievalOptions>>().Value);
    }

    public static IEnumerable<object[]> InvalidSimilarityScores()
    {
        yield return [ -0.01 ];
        yield return [ 1.01 ];
        yield return [ double.NaN ];
        yield return [ double.PositiveInfinity ];
        yield return [ double.NegativeInfinity ];
    }

    private static ServiceProvider CreateProvider(
        Dictionary<string, string?>? overrides = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:AuthDatabase"] =
                "Host=localhost;Database=RetrievalTests;Username=test;Password=test"
        };

        if (overrides is not null)
        {
            foreach (var setting in overrides)
            {
                settings[setting.Key] = setting.Value;
            }
        }

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider();
    }
}
