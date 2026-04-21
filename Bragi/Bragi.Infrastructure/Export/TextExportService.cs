using System.Text;
using Bragi.Application.Configuration;
using Bragi.Application.Contracts;
using Bragi.Domain.Results;
using Bragi.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Bragi.Infrastructure.Export;

public sealed class TextExportService : ITextExportService
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly ILogger<TextExportService> _logger;
    private readonly TextBodyBuilder _textBodyBuilder;
    private readonly BragiConfig _config;

    public TextExportService(
        ILogger<TextExportService> logger,
        TextBodyBuilder textBodyBuilder,
        BragiConfig config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _textBodyBuilder = textBodyBuilder ?? throw new ArgumentNullException(nameof(textBodyBuilder));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task<ExportResult> ExportAsync(
        CategorizationResult categorizationResult,
        DateTimeOffset exportTimestampUtc,
        Output outputOptions,
        TextTemplate textTemplate,
        IReadOnlyList<CategoryRule> categoryRules,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(categorizationResult);
        ArgumentNullException.ThrowIfNull(outputOptions);
        ArgumentNullException.ThrowIfNull(textTemplate);
        ArgumentNullException.ThrowIfNull(categoryRules);

        var outputDirectory = GetOutputDirectory(outputOptions, exportTimestampUtc);
        Directory.CreateDirectory(outputDirectory);

        var orderedRules = categoryRules
            .Where(rule => rule.Enabled)
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(rule => rule.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _logger.LogInformation(
            "Starting export stage. OutputDirectory={OutputDirectory} CategoryRuleCount={CategoryRuleCount}",
            outputDirectory,
            orderedRules.Length);

        var generatedFiles = new List<string>();
        var categoryExportLineCounts = new Dictionary<CategoryKey, int>();

        foreach (var categoryRule in orderedRules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var categoryLines = _textBodyBuilder.BuildCategoryLines(
                categorizationResult,
                categoryRule,
                textTemplate,
                _config.BehaviorOptions);

            var categoryFilePath = Path.Combine(outputDirectory, categoryRule.OutputFileName);

            await WriteLinesAsync(categoryFilePath, categoryLines, cancellationToken);

            generatedFiles.Add(categoryFilePath);
            categoryExportLineCounts[new CategoryKey(categoryRule.Key)] = categoryLines.Count;

            _logger.LogInformation(
                "Wrote category file {CategoryFilePath} with {LineCount} lines.",
                categoryFilePath,
                categoryLines.Count);
        }

        var uncategorizedLines = _textBodyBuilder.BuildUncategorizedLines(
            categorizationResult,
            textTemplate,
            _config.BehaviorOptions);

        var uncategorizedFilePath = Path.Combine(outputDirectory, outputOptions.UncategorizedFileName);

        await WriteLinesAsync(uncategorizedFilePath, uncategorizedLines, cancellationToken);

        generatedFiles.Add(uncategorizedFilePath);

        _logger.LogInformation(
            "Wrote uncategorized file {UncategorizedFilePath} with {LineCount} lines.",
            uncategorizedFilePath,
            uncategorizedLines.Count);

        var exportResult = new ExportResult(
            outputDirectory: outputDirectory,
            generatedFiles: generatedFiles,
            categoryExportLineCounts: categoryExportLineCounts,
            uncategorizedExportLineCount: uncategorizedLines.Count,
            totalExportedCategoryLines: categoryExportLineCounts.Values.Sum(),
            outputsSorted: _config.BehaviorOptions.SortOutputs,
            outputsDeduplicated: _config.BehaviorOptions.DeduplicateOutputs);

        _logger.LogInformation(
            "Completed export stage. OutputDirectory={OutputDirectory} GeneratedCategoryFileCount={GeneratedCategoryFileCount} UncategorizedLineCount={UncategorizedLineCount}",
            outputDirectory,
            orderedRules.Length,
            uncategorizedLines.Count);

        return exportResult;
    }

    private static string GetOutputDirectory(
        Output outputOptions,
        DateTimeOffset exportTimestampUtc)
    {
        var rootPath = outputOptions.RootPath.Trim();

        if (!outputOptions.CreateMonthlySubfolder)
        {
            return rootPath;
        }

        var monthlyFolderName = exportTimestampUtc.ToString(outputOptions.MonthlySubfolderFormat);

        return Path.Combine(rootPath, monthlyFolderName);
    }

    private static Task WriteLinesAsync(
        string filePath,
        IReadOnlyList<string> lines,
        CancellationToken cancellationToken)
    {
        var fileBody = lines.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, lines);

        return File.WriteAllTextAsync(
            filePath,
            fileBody,
            Utf8WithoutBom,
            cancellationToken);
    }
}
