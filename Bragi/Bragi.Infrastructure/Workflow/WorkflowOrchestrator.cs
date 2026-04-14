using System;
using System.IO;
using Bragi.Application.Configuration;
using Bragi.Application.Contracts;
using Bragi.Application.Workflow;
using Bragi.Domain.Enums;
using Bragi.Domain.Results;
using Microsoft.Extensions.Logging;

namespace Bragi.Infrastructure.Workflow;

public sealed class WorkflowOrchestrator : IWorkflowOrchestrator
{
    private readonly ILogger<WorkflowOrchestrator> _logger;
    private readonly BragiConfig _config;
    private readonly WizardSessionStore _wizardSessionStore;
    private readonly IInputIngestService _inputIngestService;
    private readonly ISubjectExtractionService _subjectExtractionService;
    private readonly ICategorizationService _categorizationService;
    private readonly ITextExportService _textExportService;

    public WorkflowOrchestrator(
        ILogger<WorkflowOrchestrator> logger,
        BragiConfig config,
        WizardSessionStore wizardSessionStore,
        IInputIngestService inputIngestService,
        ISubjectExtractionService subjectExtractionService,
        ICategorizationService categorizationService,
        ITextExportService textExportService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _wizardSessionStore = wizardSessionStore ?? throw new ArgumentNullException(nameof(wizardSessionStore));
        _inputIngestService = inputIngestService ?? throw new ArgumentNullException(nameof(inputIngestService));
        _subjectExtractionService = subjectExtractionService ?? throw new ArgumentNullException(nameof(subjectExtractionService));
        _categorizationService = categorizationService ?? throw new ArgumentNullException(nameof(categorizationService));
        _textExportService = textExportService ?? throw new ArgumentNullException(nameof(textExportService));
    }

    public async Task<ExtractionResult> PreviewExtractionAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default)
    {
        using var linkedCancellationTokenSource = BeginBusyOperation(cancellationToken);

        try
        {
            var normalizedSourceFilePath = NormalizeSourceFilePath(sourceFilePath);

            var extractionResult = await EnsureExtractionAsync(
                normalizedSourceFilePath,
                linkedCancellationTokenSource.Token);

            _logger.LogInformation(
                "Extraction preview prepared for {SourceFilePath}. Extracted subjects: {ExtractedSubjectCount}.",
                normalizedSourceFilePath,
                extractionResult.ExtractedCount);

            return extractionResult;
        }
        finally
        {
            _wizardSessionStore.CompleteBusyOperation();
        }
    }

    public async Task<CategorizationResult> PreviewCategorizationAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default)
    {
        using var linkedCancellationTokenSource = BeginBusyOperation(cancellationToken);

        try
        {
            var normalizedSourceFilePath = NormalizeSourceFilePath(sourceFilePath);

            var extractionResult = await EnsureExtractionAsync(
                normalizedSourceFilePath,
                linkedCancellationTokenSource.Token);

            _wizardSessionStore.MarkExtractionReviewComplete();

            var categorizationResult = await EnsureCategorizationAsync(
                normalizedSourceFilePath,
                extractionResult,
                linkedCancellationTokenSource.Token);

            _logger.LogInformation(
                "Categorization preview prepared for {SourceFilePath}. Categorized subjects: {CategorizedSubjectCount}. Uncategorized subjects: {UncategorizedSubjectCount}.",
                normalizedSourceFilePath,
                categorizationResult.CategorizedSubjectCount,
                categorizationResult.UncategorizedSubjectCount);

            return categorizationResult;
        }
        finally
        {
            _wizardSessionStore.CompleteBusyOperation();
        }
    }

    public async Task<RunSummary> ExecuteAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default)
    {
        using var linkedCancellationTokenSource = BeginBusyOperation(cancellationToken);

        try
        {
            var normalizedSourceFilePath = NormalizeSourceFilePath(sourceFilePath);
            var runStartedAtUtc = DateTimeOffset.UtcNow;

            var extractionResult = await EnsureExtractionAsync(
                normalizedSourceFilePath,
                linkedCancellationTokenSource.Token);

            _wizardSessionStore.MarkExtractionReviewComplete();

            var categorizationResult = await EnsureCategorizationAsync(
                normalizedSourceFilePath,
                extractionResult,
                linkedCancellationTokenSource.Token);

            var runCompletedAtUtc = DateTimeOffset.UtcNow;

            var runSummary = new RunSummary(
                sourceFile: extractionResult.SourceFile,
                inputFileKind: extractionResult.InputFileKind,
                runStartedAtUtc: runStartedAtUtc,
                runCompletedAtUtc: runCompletedAtUtc,
                totalRecordsRead: extractionResult.TotalRecordsRead,
                extractedSubjectCount: extractionResult.ExtractedCount,
                categorizedAssignmentCount: categorizationResult.TotalAssignments,
                uncategorizedSubjectCount: categorizationResult.UncategorizedSubjectCount,
                blankOrIgnoredCount: extractionResult.BlankOrIgnoredCount,
                duplicateCount: extractionResult.DuplicateCount,
                parseWarningCount: extractionResult.ParseWarningCount,
                categoryCounts: categorizationResult.CategoryCounts);

            var effectiveOutputOptions = BuildEffectiveOutputOptions();

            await _textExportService.ExportAsync(
                categorizationResult,
                runSummary,
                effectiveOutputOptions,
                _config.TextTemplate,
                _config.CategoryRules,
                linkedCancellationTokenSource.Token);

            var generatedFiles = BuildGeneratedFileList(
                effectiveOutputOptions,
                runSummary,
                _config.CategoryRules);

            _wizardSessionStore.SetRunSummary(runSummary, generatedFiles);

            _logger.LogInformation(
                "Workflow execution completed for {SourceFilePath}. Generated files: {GeneratedFileCount}.",
                normalizedSourceFilePath,
                generatedFiles.Count);

            return runSummary;
        }
        finally
        {
            _wizardSessionStore.CompleteBusyOperation();
        }
    }

    private async Task<ExtractionResult> EnsureExtractionAsync(
        string sourceFilePath,
        CancellationToken cancellationToken)
    {
        if (_wizardSessionStore.ExtractedSubjects is not null &&
            PathsMatch(_wizardSessionStore.ExtractedSubjects.SourceFile, sourceFilePath))
        {
            return _wizardSessionStore.ExtractedSubjects;
        }

        var inputFileKind = await _inputIngestService.DetectInputFileKindAsync(
            sourceFilePath,
            _config.InputOptions,
            cancellationToken);

        if (inputFileKind == InputFileKind.Unknown)
        {
            throw new InvalidOperationException($"Unsupported input file type for '{sourceFilePath}'.");
        }

        _wizardSessionStore.SetSelectedInputFile(sourceFilePath, inputFileKind);

        ExtractionResult extractionResult;

        if (inputFileKind == InputFileKind.PlainText)
        {
            var textContent = await _inputIngestService.ReadAllTextAsync(
                sourceFilePath,
                cancellationToken);

            extractionResult = await _subjectExtractionService.ExtractFromPlainTextAsync(
                sourceFilePath,
                textContent,
                _config.InputOptions,
                _config.BehaviorOptions,
                cancellationToken);
        }
        else
        {
            var csvRows = await _inputIngestService.ReadCsvRowsAsync(
                sourceFilePath,
                cancellationToken);

            extractionResult = await _subjectExtractionService.ExtractFromCsvAsync(
                sourceFilePath,
                csvRows,
                _config.CsvColumns,
                _config.InputOptions,
                _config.BehaviorOptions,
                cancellationToken);
        }

        _wizardSessionStore.SetExtractionResult(extractionResult);

        return extractionResult;
    }

    private async Task<CategorizationResult> EnsureCategorizationAsync(
        string sourceFilePath,
        ExtractionResult extractionResult,
        CancellationToken cancellationToken)
    {
        if (_wizardSessionStore.LastCategorizationResult is not null &&
            PathsMatch(extractionResult.SourceFile, sourceFilePath))
        {
            return _wizardSessionStore.LastCategorizationResult;
        }

        var categorizationResult = await _categorizationService.CategorizeAsync(
            extractionResult,
            _config.CategoryRules,
            _config.BehaviorOptions,
            cancellationToken);

        _wizardSessionStore.SetCategorizationResult(categorizationResult);

        return categorizationResult;
    }

    private Output BuildEffectiveOutputOptions()
    {
        if (string.IsNullOrWhiteSpace(_wizardSessionStore.SelectedOutputFolder))
        {
            return _config.Output;
        }

        return _config.Output with
        {
            RootPath = _wizardSessionStore.SelectedOutputFolder.Trim()
        };
    }

    private IReadOnlyList<string> BuildGeneratedFileList(
        Output outputOptions,
        RunSummary runSummary,
        IReadOnlyList<CategoryRule> categoryRules)
    {
        var outputDirectory = GetOutputDirectory(outputOptions, runSummary);

        var orderedRules = categoryRules
            .Where(rule => rule.Enabled)
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(rule => rule.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var filePaths = new List<string>();

        foreach (var rule in orderedRules)
        {
            filePaths.Add(Path.Combine(outputDirectory, rule.OutputFileName));
        }

        filePaths.Add(Path.Combine(outputDirectory, outputOptions.UncategorizedFileName));
        filePaths.Add(Path.Combine(outputDirectory, outputOptions.RunSummaryFileName));

        return filePaths;
    }

    private static string GetOutputDirectory(
        Output outputOptions,
        RunSummary runSummary)
    {
        if (!outputOptions.CreateMonthlySubfolder)
        {
            return outputOptions.RootPath;
        }

        var monthlyFolderName = runSummary.RunCompletedAtUtc.ToString(outputOptions.MonthlySubfolderFormat);

        return Path.Combine(outputOptions.RootPath, monthlyFolderName);
    }

    private CancellationTokenSource BeginBusyOperation(CancellationToken cancellationToken)
    {
        var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _wizardSessionStore.BeginBusyOperation(linkedCancellationTokenSource);

        return linkedCancellationTokenSource;
    }

    private static string NormalizeSourceFilePath(string sourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            throw new ArgumentException("Source file path cannot be null, empty, or whitespace.", nameof(sourceFilePath));
        }

        return Path.GetFullPath(sourceFilePath.Trim());
    }

    private static bool PathsMatch(string leftPath, string rightPath)
    {
        return string.Equals(
            Path.GetFullPath(leftPath),
            Path.GetFullPath(rightPath),
            StringComparison.OrdinalIgnoreCase);
    }
}
