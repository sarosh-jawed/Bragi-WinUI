using System.Diagnostics;
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
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting extraction preview. SourceFilePath={SourceFilePath}",
            sourceFilePath);

        using var linkedCancellationTokenSource = BeginBusyOperation(cancellationToken);

        try
        {
            var normalizedSourceFilePath = NormalizeSourceFilePath(sourceFilePath);

            var extractionResult = await EnsureExtractionAsync(
                normalizedSourceFilePath,
                linkedCancellationTokenSource.Token);

            stopwatch.Stop();

            _logger.LogInformation(
                "Completed extraction preview. SourceFilePath={SourceFilePath} ExtractedSubjectCount={ExtractedSubjectCount} TotalRecordsRead={TotalRecordsRead} DurationMs={DurationMs}",
                normalizedSourceFilePath,
                extractionResult.ExtractedCount,
                extractionResult.TotalRecordsRead,
                stopwatch.ElapsedMilliseconds);

            return extractionResult;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();

            _logger.LogWarning(
                "Extraction preview cancelled. SourceFilePath={SourceFilePath} DurationMs={DurationMs}",
                sourceFilePath,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Extraction preview failed. SourceFilePath={SourceFilePath} DurationMs={DurationMs}",
                sourceFilePath,
                stopwatch.ElapsedMilliseconds);

            throw;
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
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting categorization preview. SourceFilePath={SourceFilePath}",
            sourceFilePath);

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

            stopwatch.Stop();

            _logger.LogInformation(
                "Completed categorization preview. SourceFilePath={SourceFilePath} CategorizedSubjectCount={CategorizedSubjectCount} UncategorizedSubjectCount={UncategorizedSubjectCount} TotalAssignments={TotalAssignments} DurationMs={DurationMs}",
                normalizedSourceFilePath,
                categorizationResult.CategorizedSubjectCount,
                categorizationResult.UncategorizedSubjectCount,
                categorizationResult.TotalAssignments,
                stopwatch.ElapsedMilliseconds);

            return categorizationResult;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();

            _logger.LogWarning(
                "Categorization preview cancelled. SourceFilePath={SourceFilePath} DurationMs={DurationMs}",
                sourceFilePath,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Categorization preview failed. SourceFilePath={SourceFilePath} DurationMs={DurationMs}",
                sourceFilePath,
                stopwatch.ElapsedMilliseconds);

            throw;
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
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting export workflow. SourceFilePath={SourceFilePath} SelectedOutputFolder={SelectedOutputFolder}",
            sourceFilePath,
            _wizardSessionStore.SelectedOutputFolder ?? _config.Output.RootPath);

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

            stopwatch.Stop();

            _logger.LogInformation(
                "Completed export workflow. SourceFilePath={SourceFilePath} GeneratedFileCount={GeneratedFileCount} CategorizedAssignmentCount={CategorizedAssignmentCount} UncategorizedSubjectCount={UncategorizedSubjectCount} DurationMs={DurationMs}",
                normalizedSourceFilePath,
                generatedFiles.Count,
                runSummary.CategorizedAssignmentCount,
                runSummary.UncategorizedSubjectCount,
                stopwatch.ElapsedMilliseconds);

            return runSummary;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();

            _logger.LogWarning(
                "Export workflow cancelled. SourceFilePath={SourceFilePath} DurationMs={DurationMs}",
                sourceFilePath,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Export workflow failed. SourceFilePath={SourceFilePath} DurationMs={DurationMs}",
                sourceFilePath,
                stopwatch.ElapsedMilliseconds);

            throw;
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
            _logger.LogInformation(
                "Reusing cached extraction result. SourceFilePath={SourceFilePath} ExtractedSubjectCount={ExtractedSubjectCount}",
                sourceFilePath,
                _wizardSessionStore.ExtractedSubjects.ExtractedCount);

            return _wizardSessionStore.ExtractedSubjects;
        }

        _logger.LogInformation(
            "Starting extraction stage. SourceFilePath={SourceFilePath}",
            sourceFilePath);

        var inputFileKind = await _inputIngestService.DetectInputFileKindAsync(
            sourceFilePath,
            _config.InputOptions,
            cancellationToken);

        _logger.LogInformation(
            "Detected input kind. SourceFilePath={SourceFilePath} InputFileKind={InputFileKind}",
            sourceFilePath,
            inputFileKind);

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

        _logger.LogInformation(
            "Completed extraction stage. SourceFilePath={SourceFilePath} InputFileKind={InputFileKind} ExtractedSubjectCount={ExtractedSubjectCount} BlankOrIgnoredCount={BlankOrIgnoredCount} DuplicateCount={DuplicateCount} ParseWarningCount={ParseWarningCount}",
            sourceFilePath,
            extractionResult.InputFileKind,
            extractionResult.ExtractedCount,
            extractionResult.BlankOrIgnoredCount,
            extractionResult.DuplicateCount,
            extractionResult.ParseWarningCount);

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
            _logger.LogInformation(
                "Reusing cached categorization result. SourceFilePath={SourceFilePath} TotalAssignments={TotalAssignments}",
                sourceFilePath,
                _wizardSessionStore.LastCategorizationResult.TotalAssignments);

            return _wizardSessionStore.LastCategorizationResult;
        }

        _logger.LogInformation(
            "Starting categorization stage. SourceFilePath={SourceFilePath} CategoryRuleCount={CategoryRuleCount}",
            sourceFilePath,
            _config.CategoryRules.Count(rule => rule.Enabled));

        var categorizationResult = await _categorizationService.CategorizeAsync(
            extractionResult,
            _config.CategoryRules,
            _config.BehaviorOptions,
            cancellationToken);

        _wizardSessionStore.SetCategorizationResult(categorizationResult);

        _logger.LogInformation(
            "Completed categorization stage. SourceFilePath={SourceFilePath} CategorizedSubjectCount={CategorizedSubjectCount} UncategorizedSubjectCount={UncategorizedSubjectCount} TotalAssignments={TotalAssignments}",
            sourceFilePath,
            categorizationResult.CategorizedSubjectCount,
            categorizationResult.UncategorizedSubjectCount,
            categorizationResult.TotalAssignments);

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
