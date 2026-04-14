using System.IO;
using Bragi.Application.Configuration;

namespace Bragi.Infrastructure.Configuration;

public sealed class BragiConfigValidator
{
    public void Validate(BragiConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var errors = new List<string>();

        ValidateRequiredSections(config, errors);
        ValidateCsvColumns(config, errors);
        ValidateInputOptions(config, errors);
        ValidateOutput(config, errors);
        ValidateTextTemplate(config, errors);
        ValidateCategoryRules(config, errors);

        if (errors.Count > 0)
        {
            var message =
                "Bragi configuration is invalid." +
                Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select(error => $"- {error}"));

            throw new InvalidOperationException(message);
        }
    }

    private static void ValidateRequiredSections(BragiConfig config, List<string> errors)
    {
        if (config.CategoryRules is null || config.CategoryRules.Count == 0)
        {
            errors.Add("CategoryRules must contain at least one category rule.");
        }

        if (config.CsvColumns is null)
        {
            errors.Add("CsvColumns section is required.");
        }

        if (config.InputOptions is null)
        {
            errors.Add("InputOptions section is required.");
        }

        if (config.Output is null)
        {
            errors.Add("Output section is required.");
        }

        if (config.TextTemplate is null)
        {
            errors.Add("TextTemplate section is required.");
        }

        if (config.BehaviorOptions is null)
        {
            errors.Add("BehaviorOptions section is required.");
        }
    }

    private static void ValidateCsvColumns(BragiConfig config, List<string> errors)
    {
        var csvColumns = config.CsvColumns;

        if (string.IsNullOrWhiteSpace(csvColumns.SubjectColumnName))
        {
            errors.Add("CsvColumns.SubjectColumnName is required.");
        }

        if (config.InputOptions.CaptureCsvSourceTitle && string.IsNullOrWhiteSpace(csvColumns.TitleColumnName))
        {
            errors.Add("InputOptions.CaptureCsvSourceTitle is enabled, but CsvColumns.TitleColumnName is missing.");
        }

        if (config.InputOptions.CaptureCsvSourceRecordId && string.IsNullOrWhiteSpace(csvColumns.RecordIdColumnName))
        {
            errors.Add("InputOptions.CaptureCsvSourceRecordId is enabled, but CsvColumns.RecordIdColumnName is missing.");
        }

        var enabledColumnNames = new List<string>();

        if (!string.IsNullOrWhiteSpace(csvColumns.SubjectColumnName))
        {
            enabledColumnNames.Add(csvColumns.SubjectColumnName);
        }

        if (config.InputOptions.CaptureCsvSourceTitle && !string.IsNullOrWhiteSpace(csvColumns.TitleColumnName))
        {
            enabledColumnNames.Add(csvColumns.TitleColumnName!);
        }

        if (config.InputOptions.CaptureCsvSourceRecordId && !string.IsNullOrWhiteSpace(csvColumns.RecordIdColumnName))
        {
            enabledColumnNames.Add(csvColumns.RecordIdColumnName!);
        }

        var duplicateColumnNames = enabledColumnNames
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var duplicateColumnName in duplicateColumnNames)
        {
            errors.Add($"CSV column setup is invalid. Duplicate column name detected: '{duplicateColumnName}'.");
        }
    }

    private static void ValidateInputOptions(BragiConfig config, List<string> errors)
    {
        if (config.InputOptions.PlainTextExtensions is null || config.InputOptions.PlainTextExtensions.Count == 0)
        {
            errors.Add("InputOptions.PlainTextExtensions must contain at least one extension.");
        }

        if (config.InputOptions.CsvExtensions is null || config.InputOptions.CsvExtensions.Count == 0)
        {
            errors.Add("InputOptions.CsvExtensions must contain at least one extension.");
        }
    }

    private static void ValidateOutput(BragiConfig config, List<string> errors)
    {
        var output = config.Output;

        if (string.IsNullOrWhiteSpace(output.RootPath))
        {
            errors.Add("Output.RootPath is required.");
        }

        if (!IsValidFileName(output.UncategorizedFileName))
        {
            errors.Add("Output.UncategorizedFileName is missing or invalid.");
        }

        if (!IsValidFileName(output.RunSummaryFileName))
        {
            errors.Add("Output.RunSummaryFileName is missing or invalid.");
        }

        if (output.CreateMonthlySubfolder && string.IsNullOrWhiteSpace(output.MonthlySubfolderFormat))
        {
            errors.Add("Output.MonthlySubfolderFormat is required when CreateMonthlySubfolder is enabled.");
        }
    }

    private static void ValidateTextTemplate(BragiConfig config, List<string> errors)
    {
        var textTemplate = config.TextTemplate;

        if (string.IsNullOrWhiteSpace(textTemplate.CategoryLineTemplate))
        {
            errors.Add("TextTemplate.CategoryLineTemplate is required.");
        }

        if (string.IsNullOrWhiteSpace(textTemplate.UncategorizedLineTemplate))
        {
            errors.Add("TextTemplate.UncategorizedLineTemplate is required.");
        }

        if (string.IsNullOrWhiteSpace(textTemplate.RunSummaryHeaderTemplate))
        {
            errors.Add("TextTemplate.RunSummaryHeaderTemplate is required.");
        }

        if (string.IsNullOrWhiteSpace(textTemplate.RunSummaryCategoryLineTemplate))
        {
            errors.Add("TextTemplate.RunSummaryCategoryLineTemplate is required.");
        }

        if (string.IsNullOrWhiteSpace(textTemplate.RunSummaryDetailLineTemplate))
        {
            errors.Add("TextTemplate.RunSummaryDetailLineTemplate is required.");
        }
    }

    private static void ValidateCategoryRules(BragiConfig config, List<string> errors)
    {
        var rules = config.CategoryRules ?? [];

        foreach (var rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Key))
            {
                errors.Add("A category rule is missing Key.");
            }

            if (string.IsNullOrWhiteSpace(rule.DisplayName))
            {
                errors.Add($"Category rule '{rule.Key}' is missing DisplayName.");
            }

            if (!IsValidFileName(rule.OutputFileName))
            {
                errors.Add($"Category rule '{rule.Key}' has a missing or invalid OutputFileName.");
            }

            if (rule.IncludeKeywords is null || rule.IncludeKeywords.Count == 0)
            {
                errors.Add($"Category rule '{rule.Key}' must contain at least one IncludeKeyword.");
            }
        }

        var duplicateKeys = rules
            .Where(rule => !string.IsNullOrWhiteSpace(rule.Key))
            .GroupBy(rule => rule.Key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var duplicateKey in duplicateKeys)
        {
            errors.Add($"Duplicate category key detected: '{duplicateKey}'.");
        }

        var duplicateOutputFileNames = rules
            .Where(rule => !string.IsNullOrWhiteSpace(rule.OutputFileName))
            .GroupBy(rule => rule.OutputFileName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(fileName => fileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var duplicateOutputFileName in duplicateOutputFileNames)
        {
            errors.Add($"Duplicate category output file name detected: '{duplicateOutputFileName}'.");
        }
    }

    private static bool IsValidFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var trimmedValue = fileName.Trim();

        if (trimmedValue.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return false;
        }

        if (trimmedValue.Contains(Path.DirectorySeparatorChar) || trimmedValue.Contains(Path.AltDirectorySeparatorChar))
        {
            return false;
        }

        return true;
    }
}
