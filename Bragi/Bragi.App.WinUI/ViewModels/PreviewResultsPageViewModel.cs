using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Bragi.App.WinUI.ViewModels.Base;
using Bragi.Application.Configuration;
using Bragi.Application.Contracts;
using Bragi.Application.Errors;
using Bragi.Application.Workflow;
using Bragi.Domain.Results;
using Microsoft.Extensions.Logging;

namespace Bragi.App.WinUI.ViewModels;

public sealed class PreviewResultsPageViewModel : ObservableObject
{
    private const int CategorySubjectPreviewLimit = 25;
    private const int UncategorizedPreviewLimit = 100;

    private readonly IWorkflowOrchestrator _workflowOrchestrator;
    private readonly WizardSessionStore _wizardSessionStore;
    private readonly BragiConfig _config;
    private readonly ILogger<PreviewResultsPageViewModel> _logger;

    private CategorizationResult? _lastBoundCategorizationResult;
    private bool _isLoadingPreview;
    private string _statusMessage = "Complete subject review before generating the categorization preview.";
    private string _categoryPreviewWindowText = "No category preview is currently available.";
    private string _uncategorizedPreviewWindowText = "No uncategorized preview is currently available.";
    private int _uncategorizedCount;
    private int _totalAssignments;

    public PreviewResultsPageViewModel(
        IWorkflowOrchestrator workflowOrchestrator,
        WizardSessionStore wizardSessionStore,
        BragiConfig config,
        ILogger<PreviewResultsPageViewModel> logger)
    {
        _workflowOrchestrator = workflowOrchestrator ?? throw new ArgumentNullException(nameof(workflowOrchestrator));
        _wizardSessionStore = wizardSessionStore ?? throw new ArgumentNullException(nameof(wizardSessionStore));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        CategoryCounts = new ObservableCollection<CategoryCountItem>();
        CategoryGroups = new ObservableCollection<CategoryGroupItem>();
        UncategorizedSubjects = new ObservableCollection<UncategorizedItem>();

        _wizardSessionStore.SessionChanged += OnSessionChanged;
        RefreshFromSession();
    }

    public ObservableCollection<CategoryCountItem> CategoryCounts { get; }

    public ObservableCollection<CategoryGroupItem> CategoryGroups { get; }

    public ObservableCollection<UncategorizedItem> UncategorizedSubjects { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string CategoryPreviewWindowText
    {
        get => _categoryPreviewWindowText;
        private set => SetProperty(ref _categoryPreviewWindowText, value);
    }

    public string UncategorizedPreviewWindowText
    {
        get => _uncategorizedPreviewWindowText;
        private set => SetProperty(ref _uncategorizedPreviewWindowText, value);
    }

    public int UncategorizedCount
    {
        get => _uncategorizedCount;
        private set => SetProperty(ref _uncategorizedCount, value);
    }

    public int TotalAssignments
    {
        get => _totalAssignments;
        private set => SetProperty(ref _totalAssignments, value);
    }

    public bool HasPreview => CategoryGroups.Count > 0 || UncategorizedSubjects.Count > 0;

    public bool CanGeneratePreview =>
        !_wizardSessionStore.State.IsBusy &&
        _wizardSessionStore.State.IsExtractionReviewComplete &&
        !string.IsNullOrWhiteSpace(_wizardSessionStore.SelectedInputFile);

    public async Task EnsurePreviewAsync()
    {
        if (_isLoadingPreview)
        {
            return;
        }

        if (_wizardSessionStore.LastCategorizationResult is not null)
        {
            RefreshFromSession();
            return;
        }

        if (!CanGeneratePreview)
        {
            RefreshFromSession();
            return;
        }

        var sourceFilePath = _wizardSessionStore.SelectedInputFile;

        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            StatusMessage = "No input file is currently selected.";
            return;
        }

        try
        {
            _isLoadingPreview = true;
            StatusMessage = "Generating categorization preview...";

            await _workflowOrchestrator.PreviewCategorizationAsync(sourceFilePath);

            RefreshFromSession();
            StatusMessage = "Preview generated successfully. Continue to Export & Finish when ready.";
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Preview generation was cancelled.");
            RefreshFromSession();
            StatusMessage = "Preview generation was cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate preview results.");
            RefreshFromSession();
            StatusMessage = UserMessageHelper.ForPreview(ex);
        }
        finally
        {
            _isLoadingPreview = false;
            OnPropertyChanged(nameof(CanGeneratePreview));
        }
    }

    public void RefreshFromSession()
    {
        var categorizationResult = _wizardSessionStore.LastCategorizationResult;

        UncategorizedCount = categorizationResult?.UncategorizedSubjectCount ?? 0;
        TotalAssignments = categorizationResult?.TotalAssignments ?? 0;

        if (!_wizardSessionStore.State.IsExtractionReviewComplete)
        {
            StatusMessage = "Complete subject review before generating the categorization preview.";
        }
        else if (categorizationResult is null)
        {
            StatusMessage = "Generate or refresh the categorization preview.";
        }

        if (categorizationResult is null)
        {
            ClearPreviewCollections();
            _lastBoundCategorizationResult = null;
            CategoryPreviewWindowText = "No category preview is currently available.";
            UncategorizedPreviewWindowText = "No uncategorized preview is currently available.";

            OnPropertyChanged(nameof(HasPreview));
            OnPropertyChanged(nameof(CanGeneratePreview));
            return;
        }

        var displayNameLookup = _config.CategoryRules
            .ToDictionary(
                rule => rule.Key,
                rule => string.IsNullOrWhiteSpace(rule.DisplayName) ? rule.Key : rule.DisplayName,
                StringComparer.OrdinalIgnoreCase);

        // The processing layer already caches categorization results.
        // The performance problem with large files is mostly UI-side: this view model
        // used to rebuild every category group and every uncategorized item on every
        // session change. Rebuild only when the actual categorization result changes.
        if (!ReferenceEquals(_lastBoundCategorizationResult, categorizationResult) ||
            CategoryCounts.Count == 0)
        {
            RebuildCategoryCounts(displayNameLookup, categorizationResult);
            RebuildCategoryGroups(displayNameLookup, categorizationResult);
            RebuildUncategorizedSubjects(categorizationResult);

            _lastBoundCategorizationResult = categorizationResult;
        }

        StatusMessage = "Preview results are ready.";
        OnPropertyChanged(nameof(HasPreview));
        OnPropertyChanged(nameof(CanGeneratePreview));
    }

    private void RebuildCategoryCounts(
        IReadOnlyDictionary<string, string> displayNameLookup,
        CategorizationResult categorizationResult)
    {
        CategoryCounts.Clear();

        foreach (var categoryCount in categorizationResult.CategoryCounts
                     .OrderBy(pair => GetSortOrder(pair.Key.Value))
                     .ThenBy(pair => GetDisplayName(displayNameLookup, pair.Key.Value), StringComparer.OrdinalIgnoreCase))
        {
            CategoryCounts.Add(new CategoryCountItem(
                GetDisplayName(displayNameLookup, categoryCount.Key.Value),
                categoryCount.Key.Value,
                categoryCount.Value));
        }
    }

    private void RebuildCategoryGroups(
        IReadOnlyDictionary<string, string> displayNameLookup,
        CategorizationResult categorizationResult)
    {
        CategoryGroups.Clear();

        var groupedAssignments = categorizationResult.CategorizedSubjects
            .SelectMany(subject => subject.Matches.Select(match => new
            {
                CategoryKey = match.CategoryKey.Value,
                CategoryDisplayName = GetDisplayName(displayNameLookup, match.CategoryKey.Value),
                SubjectText = subject.Subject.OriginalSubject.Value,
                Reason = match.Reason,
                SourceRowNumber = subject.Subject.Entry.SourceRowNumber
            }))
            .GroupBy(item => item.CategoryKey, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => GetSortOrder(group.Key))
            .ThenBy(group => GetDisplayName(displayNameLookup, group.Key), StringComparer.OrdinalIgnoreCase);

        foreach (var group in groupedAssignments)
        {
            var orderedItems = group
                .OrderBy(item => item.SourceRowNumber ?? int.MaxValue)
                .ThenBy(item => item.SubjectText, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var previewItems = orderedItems
                .Take(CategorySubjectPreviewLimit)
                .Select(item => new CategorySubjectItem(
                    item.SubjectText,
                    item.Reason,
                    item.SourceRowNumber.HasValue ? $"Row {item.SourceRowNumber.Value}" : "No row metadata"));

            CategoryGroups.Add(new CategoryGroupItem(
                GetDisplayName(displayNameLookup, group.Key),
                group.Key,
                orderedItems.Count,
                BuildCategoryGroupPreviewSummary(orderedItems.Count),
                new ObservableCollection<CategorySubjectItem>(previewItems)));
        }

        CategoryPreviewWindowText = CategoryGroups.Count == 0
            ? "No category preview is currently available."
            : $"Showing up to {CategorySubjectPreviewLimit} subjects per category group.";
    }

    private void RebuildUncategorizedSubjects(CategorizationResult categorizationResult)
    {
        UncategorizedSubjects.Clear();

        var orderedUncategorized = categorizationResult.UncategorizedSubjects
            .OrderBy(item => item.Subject.SequenceNumber)
            .ToList();

        foreach (var uncategorizedSubject in orderedUncategorized.Take(UncategorizedPreviewLimit))
        {
            UncategorizedSubjects.Add(new UncategorizedItem(
                uncategorizedSubject.Subject.OriginalSubject.Value,
                uncategorizedSubject.Reason,
                uncategorizedSubject.Subject.Entry.SourceRowNumber.HasValue
                    ? $"Row {uncategorizedSubject.Subject.Entry.SourceRowNumber.Value}"
                    : "No row metadata"));
        }

        UncategorizedPreviewWindowText = orderedUncategorized.Count switch
        {
            0 => "No uncategorized subjects in this preview.",
            <= UncategorizedPreviewLimit => $"Showing all {orderedUncategorized.Count} uncategorized subjects.",
            _ => $"Showing first {UncategorizedPreviewLimit} of {orderedUncategorized.Count} uncategorized subjects."
        };
    }

    private void ClearPreviewCollections()
    {
        CategoryCounts.Clear();
        CategoryGroups.Clear();
        UncategorizedSubjects.Clear();
    }

    private static string BuildCategoryGroupPreviewSummary(int totalItemCount)
    {
        return totalItemCount <= CategorySubjectPreviewLimit
            ? $"Showing all {totalItemCount} subjects in this category preview."
            : $"Showing first {CategorySubjectPreviewLimit} of {totalItemCount} subjects in this category preview.";
    }

    private int GetSortOrder(string categoryKey)
    {
        var matchedRule = _config.CategoryRules.FirstOrDefault(
            rule => string.Equals(rule.Key, categoryKey, StringComparison.OrdinalIgnoreCase));

        return matchedRule?.SortOrder ?? int.MaxValue;
    }

    private static string GetDisplayName(
        IReadOnlyDictionary<string, string> displayNameLookup,
        string categoryKey)
    {
        return displayNameLookup.TryGetValue(categoryKey, out var displayName)
            ? displayName
            : categoryKey;
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        RefreshFromSession();
    }

    public sealed record CategoryCountItem(
        string DisplayName,
        string CategoryKey,
        int Count);

    public sealed record CategoryGroupItem(
        string DisplayName,
        string CategoryKey,
        int Count,
        string PreviewSummary,
        ObservableCollection<CategorySubjectItem> Subjects);

    public sealed record CategorySubjectItem(
        string SubjectText,
        string Reason,
        string SourceMetadata);

    public sealed record UncategorizedItem(
        string SubjectText,
        string Reason,
        string SourceMetadata);
}
