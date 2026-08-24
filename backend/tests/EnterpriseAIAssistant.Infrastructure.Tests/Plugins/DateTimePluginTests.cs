using EnterpriseAIAssistant.Infrastructure.AI.Plugins;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Plugins;

public class DateTimePluginTests
{
    // Test 1 : La fonction retourne une valeur valide (non nulle et non vide)
    [Fact]
    public void GetDateTime_ReturnsNonEmptyString()
    {
        // Act
        var result = DateTimePlugin.GetDateTime();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    // Test 1 (bis) : La valeur retournée est parseable en DateTime
    [Fact]
    public void GetDateTime_ReturnsValidDateTimeString()
    {
        // Act
        var result = DateTimePlugin.GetDateTime();

        // Assert
        bool isParseable = DateTime.TryParseExact(
            result,
            "yyyy-MM-dd HH:mm:ss",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out _);

        Assert.True(isParseable, $"La valeur '{result}' n'est pas au format 'yyyy-MM-dd HH:mm:ss'.");
    }

    // Test 2 : La fonction peut être appelée indépendamment du LLM (méthode statique, sans injection)
    [Fact]
    public void GetDateTime_CanBeCalledWithoutLLM()
    {
        // Act — appel direct sans aucun service IA, kernel ou dépendance externe
        var exception = Record.Exception(() => DateTimePlugin.GetDateTime());

        // Assert
        Assert.Null(exception);
    }

    // Test 3 : Le plugin ne dépend pas d'une API externe (instanciable sans dépendances)
    [Fact]
    public void DateTimePlugin_HasNoDependencies_CanBeInstantiatedDirectly()
    {
        // Act
        var plugin = new DateTimePlugin();

        // Assert
        Assert.NotNull(plugin);
    }

    // Test 3 (bis) : La méthode est statique — aucune injection requise
    [Fact]
    public void GetDateTime_IsStaticMethod_RequiresNoExternalDependency()
    {
        // Arrange
        var method = typeof(DateTimePlugin).GetMethod(nameof(DateTimePlugin.GetDateTime));

        // Assert
        Assert.NotNull(method);
        Assert.True(method!.IsStatic, "GetDateTime doit être une méthode statique sans dépendance externe.");
    }

    // Test 4 : Le comportement attendu est conservé — format exact "yyyy-MM-dd HH:mm:ss"
    [Fact]
    public void GetDateTime_ReturnsExpectedFormat()
    {
        // Arrange
        var before = DateTime.Now;

        // Act
        var result = DateTimePlugin.GetDateTime();

        var after = DateTime.Now;

        // Assert — format correct
        Assert.Matches(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$", result);

        // Assert — valeur cohérente avec l'heure actuelle
        var parsed = DateTime.ParseExact(
            result,
            "yyyy-MM-dd HH:mm:ss",
            System.Globalization.CultureInfo.InvariantCulture);

        Assert.InRange(parsed, before.AddSeconds(-1), after.AddSeconds(1));
    }

    // Test 4 (bis) : Deux appels successifs retournent des valeurs du même jour
    [Fact]
    public void GetDateTime_TwoConsecutiveCalls_ReturnSameDate()
    {
        // Act
        var result1 = DateTimePlugin.GetDateTime();
        var result2 = DateTimePlugin.GetDateTime();

        // Assert
        var date1 = result1[..10]; // "yyyy-MM-dd"
        var date2 = result2[..10];

        Assert.Equal(date1, date2);
    }
}
