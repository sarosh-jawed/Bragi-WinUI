using Bragi.Application.Configuration;
using Microsoft.Extensions.Configuration;

namespace Bragi.Infrastructure.Configuration;

public sealed class BragiConfigLoader
{
    private readonly IConfiguration _configuration;
    private readonly PathTokenResolver _pathTokenResolver;

    public BragiConfigLoader(
        IConfiguration configuration,
        PathTokenResolver pathTokenResolver)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _pathTokenResolver = pathTokenResolver ?? throw new ArgumentNullException(nameof(pathTokenResolver));
    }

    public BragiConfig Load()
    {
        var section = _configuration.GetSection("Bragi");

        if (!section.Exists())
        {
            throw new InvalidOperationException("Configuration section 'Bragi' was not found.");
        }

        var config = section.Get<BragiConfig>();

        if (config is null)
        {
            throw new InvalidOperationException("Configuration section 'Bragi' could not be bound to BragiConfig.");
        }

        return Normalize(config);
    }

    private BragiConfig Normalize(BragiConfig config)
    {
        var normalizedCsvColumns = config.CsvColumns with
        {
            SubjectColumnName = TrimToEmpty(config.CsvColumns.SubjectColumnName),
            TitleColumnName = NullIfWhiteSpace(config.CsvColumns.TitleColumnName),
            RecordIdColumnName = NullIfWhiteSpace(config.CsvColumns.RecordIdColumnName)
        };

        var normalizedInputOptions = config.InputOptions with
        {
            PlainTextExtensions = NormalizeExtensions(config.InputOptions.PlainTextExtensions),
            CsvExtensions = NormalizeExtensions(config.InputOptions.CsvExtensions)
        };

        var normalizedCategoryRules = config.CategoryRules
            .Select(rule => rule with
            {
                Key = TrimToEmpty(rule.Key),
                DisplayName = TrimToEmpty(rule.DisplayName),
                OutputFileName = TrimToEmpty(rule.OutputFileName),
                IncludeKeywords = NormalizeStringList(rule.IncludeKeywords),
                ExcludeKeywords = NormalizeStringList(rule.ExcludeKeywords),
                RequireAnyKeywords = NormalizeStringList(rule.RequireAnyKeywords)
            })
            .ToList();

        var normalizedOutput = config.Output with
        {
            RootPath = _pathTokenResolver.Resolve(config.Output.RootPath),
            UncategorizedFileName = TrimToEmpty(config.Output.UncategorizedFileName),
            RunSummaryFileName = TrimToEmpty(config.Output.RunSummaryFileName),
            MonthlySubfolderFormat = TrimToEmpty(config.Output.MonthlySubfolderFormat)
        };

        var normalizedTextTemplate = config.TextTemplate with
        {
            CategoryLineTemplate = TrimToEmpty(config.TextTemplate.CategoryLineTemplate),
            UncategorizedLineTemplate = TrimToEmpty(config.TextTemplate.UncategorizedLineTemplate),
            RunSummaryHeaderTemplate = TrimToEmpty(config.TextTemplate.RunSummaryHeaderTemplate),
            RunSummaryCategoryLineTemplate = TrimToEmpty(config.TextTemplate.RunSummaryCategoryLineTemplate),
            RunSummaryDetailLineTemplate = TrimToEmpty(config.TextTemplate.RunSummaryDetailLineTemplate)
        };

        return config with
        {
            CsvColumns = normalizedCsvColumns,
            InputOptions = normalizedInputOptions,
            CategoryRules = normalizedCategoryRules,
            Output = normalizedOutput,
            TextTemplate = normalizedTextTemplate
        };
    }

    private static List<string> NormalizeExtensions(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return [];
        }

        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Select(value => value.StartsWith('.') ? value : $".{value}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> NormalizeStringList(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return [];
        }

        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string TrimToEmpty(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
