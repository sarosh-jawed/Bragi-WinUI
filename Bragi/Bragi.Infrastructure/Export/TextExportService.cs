using System.Text;
using Bragi.Application.Configuration;
using Bragi.Application.Contracts;
using Bragi.Domain.Results;
using Microsoft.Extensions.Logging;

namespace Bragi.Infrastructure.Export;

public sealed class TextExportService : ITextExportService
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly ILogger<TextExportService> _logger;
    private readonly TextBodyBuilder _textBodyBuilder;
    private readonly RunSummaryBuilder _runSummaryBuilder;
    private readonly BragiConfig _config;

    public TextExportService(
        ILogger<TextExportService> logger,
        TextBodyBuilder textBodyBuilder,
        RunSummaryBuilder runSummaryBuilder,
        BragiConfig config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _textBodyBuilder = textBodyBuilder ?? throw new ArgumentNullException(nameof(textBodyBuilder));
        _runSummaryBuilder = runSummaryBuilder ?? throw new ArgumentNullException(nameof(runSummaryBuilder));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task ExportAsync(
        CategorizationResult categorizationResult,
        RunSummary runSummary,
        Output outputOptions,
        TextTemplate textTemplate,
        IReadOnlyList<CategoryRule> categoryRules,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(categorizationResult);
        ArgumentNullException.ThrowIfNull(runSummary);
        ArgumentNullException.ThrowIfNull(outputOptions);
        ArgumentNullException.ThrowIfNull(textTemplate);
        ArgumentNullException.ThrowIfNull(categoryRules);

        var outputDirectory = GetOutputDirectory(outputOptions, runSummary);
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

        _logger.LogInformation(
            "Wrote uncategorized file {UncategorizedFilePath} with {LineCount} lines.",
            uncategorizedFilePath,
            uncategorizedLines.Count);

        var runSummaryText = _runSummaryBuilder.Build(
            runSummary,
            textTemplate,
            orderedRules);

        var runSummaryFilePath = Path.Combine(outputDirectory, outputOptions.RunSummaryFileName);

        await File.WriteAllTextAsync(
            runSummaryFilePath,
            runSummaryText,
            Utf8WithoutBom,
            cancellationToken);

        _logger.LogInformation(
            "Wrote run summary file {RunSummaryFilePath}.",
            runSummaryFilePath);

        _logger.LogInformation(
            "Completed export stage. OutputDirectory={OutputDirectory} GeneratedCategoryFileCount={GeneratedCategoryFileCount} UncategorizedLineCount={UncategorizedLineCount}",
            outputDirectory,
            orderedRules.Length,
            uncategorizedLines.Count);
    }

    private static string GetOutputDirectory(
        Output outputOptions,
        RunSummary runSummary)
    {
        var rootPath = outputOptions.RootPath.Trim();

        if (!outputOptions.CreateMonthlySubfolder)
        {
            return rootPath;
        }

        var monthlyFolderName = runSummary.RunCompletedAtUtc.ToString(outputOptions.MonthlySubfolderFormat);

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
