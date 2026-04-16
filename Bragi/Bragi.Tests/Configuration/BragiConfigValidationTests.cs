using Bragi.Application.Configuration;
using Bragi.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace Bragi.Tests.Configuration;

public sealed class BragiConfigValidationTests
{
    [Fact]
    public void ActualAppConfig_Loads_And_Validates()
    {
        var config = LoadActualAppConfig();

        Assert.NotNull(config);
        Assert.NotEmpty(config.CategoryRules);

        var validator = new BragiConfigValidator();
        validator.Validate(config);
    }

    [Fact]
    public void ActualAppConfig_Contains_FullLegacyRuleSet()
    {
        var config = LoadActualAppConfig();

        var keys = config.CategoryRules
            .Select(rule => rule.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var expectedKeys = new[]
        {
            "art",
            "biology",
            "business",
            "chemistry",
            "computer",
            "education",
            "fiction",
            "forensics",
            "geoscience",
            "history",
            "hper",
            "humanities",
            "idt",
            "interdis",
            "math",
            "music",
            "nursing",
            "performance",
            "physics",
            "psych",
            "politics",
            "slim"
        };

        Assert.All(expectedKeys, expectedKey => Assert.Contains(expectedKey, keys));
    }

    [Fact]
    public void ActualAppConfig_CategoryOutputFileNames_AreUnique()
    {
        var config = LoadActualAppConfig();

        var duplicateOutputNames = config.CategoryRules
            .GroupBy(rule => rule.OutputFileName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Assert.Empty(duplicateOutputNames);
    }

    private static BragiConfig LoadActualAppConfig()
    {
        var bragiRoot = FindBragiRoot();
        var configPath = Path.Combine(bragiRoot, "Bragi.App.WinUI", "config.json");

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false, reloadOnChange: false)
            .Build();

        var loader = new BragiConfigLoader(configuration, new PathTokenResolver());
        return loader.Load();
    }

    private static string FindBragiRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var solutionPath = Path.Combine(current.FullName, "Bragi.sln");

            if (File.Exists(solutionPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Bragi solution root from the test output directory.");
    }
}
