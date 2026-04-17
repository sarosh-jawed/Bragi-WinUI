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
    private const int SelectedCategoryPreviewLimit = 12;
    private const int UncategorizedPreviewLimit = 40;

    private readonly IWorkflowOrchestrator _workflowOrchestrator;
    private readonly WizardSessionStore _wizardSessionStore;
    private readonly BragiConfig _config;
    private readonly ILogger<PreviewResultsPageViewModel> _logger;

    private readonly Dictionary<string, IReadOnlyList<CategorySubjectItem>> _categoryPreviewLookup =
        new(StringComparer.OrdinalIgnoreCase);

    private CategorizationResult? _lastBoundCategorizationResult;
    private bool _isLoadingPreview;
    private string _statusMessage = "Complete subject review before generating the categorization preview.";
    private string _categoryPreviewWindowText = "No category preview is currently available.";
    private string _uncategorizedPreviewWindowText = "No uncategorized preview is currently available.";
    private int _uncategorizedCount;
    private int _totalAssignments;
    private CategoryGroupOptionItem? _selectedCategoryGroup;

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
        CategoryGroups = new ObservableCollection<CategoryGroupOptionItem>();
        SelectedCategorySubjects = new ObservableCollection<CategorySubjectItem>();
        UncategorizedSubjects = new ObservableCollection<UncategorizedItem>();

        _wizardSessionStore.SessionChanged += OnSessionChanged;
        RefreshFromSession();
    }

    public ObservableCollection<CategoryCountItem> CategoryCounts { get; }

    public ObservableCollection<CategoryGroupOptionItem> CategoryGroups { get; }

    public ObservableCollection<CategorySubjectItem> SelectedCategorySubjects { get; }

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

    public CategoryGroupOptionItem? SelectedCategoryGroup
    {
        get => _selectedCategoryGroup;
        set
        {
            if (SetProperty(ref _selectedCategoryGroup, value))
            {
                RebuildSelectedCategorySubjects();
                OnPropertyChanged(nameof(SelectedCategoryPreviewTitle));
                OnPropertyChanged(nameof(SelectedCategoryPreviewSummary));
            }
        }
    }

    public string SelectedCategoryPreviewTitle =>
        SelectedCategoryGroup is null
            ? "Category preview"
            : $"{SelectedCategoryGroup.DisplayName} preview";

    public string SelectedCategoryPreviewSummary =>
        SelectedCategoryGroup?.PreviewSummary
        ?? "Choose a category to inspect its preview.";

    public bool HasPreview => CategoryCounts.Count > 0 || UncategorizedSubjects.Count > 0;

    public bool HasSelectedCategory => SelectedCategoryGroup is not null;

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
            ClearPreviewState();
            OnPropertyChanged(nameof(HasPreview));
            OnPropertyChanged(nameof(HasSelectedCategory));
            OnPropertyChanged(nameof(CanGeneratePreview));
            return;
        }

        // Reuse the already-bound preview state whenever the underlying categorization
        // result object has not changed. The heavy work here should only happen when a
        // genuinely new preview result is produced.
        if (!ReferenceEquals(_lastBoundCategorizationResult, categorizationResult) ||
            CategoryCounts.Count == 0)
        {
            RebuildPreviewState(categorizationResult);
            _lastBoundCategorizationResult = categorizationResult;
        }

        StatusMessage = "Preview results are ready.";
        OnPropertyChanged(nameof(HasPreview));
        OnPropertyChanged(nameof(HasSelectedCategory));
        OnPropertyChanged(nameof(CanGeneratePreview));
    }

    private void RebuildPreviewState(CategorizationResult categorizationResult)
    {
        CategoryCounts.Clear();
        CategoryGroups.Clear();
        SelectedCategorySubjects.Clear();
        UncategorizedSubjects.Clear();
        _categoryPreviewLookup.Clear();

        var displayNameLookup = _config.CategoryRules
            .ToDictionary(
                rule => rule.Key,
                rule => string.IsNullOrWhiteSpace(rule.DisplayName) ? rule.Key : rule.DisplayName,
                StringComparer.OrdinalIgnoreCase);

        foreach (var categoryCount in categorizationResult.CategoryCounts
                     .OrderBy(pair => GetSortOrder(pair.Key.Value))
                     .ThenBy(pair => GetDisplayName(displayNameLookup, pair.Key.Value), StringComparer.OrdinalIgnoreCase))
        {
            CategoryCounts.Add(new CategoryCountItem(
                GetDisplayName(displayNameLookup, categoryCount.Key.Value),
                categoryCount.Key.Value,
                categoryCount.Value));
        }

        var groupedAssignments = categorizationResult.CategorizedSubjects
            .SelectMany(subject => subject.Matches.Select(match => new
            {
                CategoryKey = match.CategoryKey.Value,
                SubjectText = subject.Subject.OriginalSubject.Value,
                Reason = match.Reason,
                SourceRowNumber = subject.Subject.Entry.SourceRowNumber
            }))
            .GroupBy(item => item.CategoryKey, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => GetSortOrder(group.Key))
            .ThenBy(group => GetDisplayName(displayNameLookup, group.Key), StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var group in groupedAssignments)
        {
            var orderedItems = group
                .OrderBy(item => item.SourceRowNumber ?? int.MaxValue)
                .ThenBy(item => item.SubjectText, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var previewItems = orderedItems
                .Take(SelectedCategoryPreviewLimit)
                .Select(item => new CategorySubjectItem(
                    item.SubjectText,
                    item.Reason,
                    item.SourceRowNumber.HasValue ? $"Row {item.SourceRowNumber.Value}" : "No row metadata"))
                .ToArray();

            _categoryPreviewLookup[group.Key] = previewItems;

            CategoryGroups.Add(new CategoryGroupOptionItem(
                GetDisplayName(displayNameLookup, group.Key),
                group.Key,
                orderedItems.Count,
                BuildCategoryPreviewSummary(orderedItems.Count)));
        }

        var previouslySelectedKey = SelectedCategoryGroup?.CategoryKey;

        SelectedCategoryGroup =
            !string.IsNullOrWhiteSpace(previouslySelectedKey)
                ? CategoryGroups.FirstOrDefault(item =>
                    string.Equals(item.CategoryKey, previouslySelectedKey, StringComparison.OrdinalIgnoreCase))
                : CategoryGroups.FirstOrDefault();

        var orderedUncategorized = categorizationResult.UncategorizedSubjects
            .OrderBy(item => item.Subject.SequenceNumber)
            .Take(UncategorizedPreviewLimit)
            .Select(item => new UncategorizedItem(
                item.Subject.OriginalSubject.Value,
                item.Reason,
                item.Subject.Entry.SourceRowNumber.HasValue
                    ? $"Row {item.Subject.Entry.SourceRowNumber.Value}"
                    : "No row metadata"));

        foreach (var uncategorizedItem in orderedUncategorized)
        {
            UncategorizedSubjects.Add(uncategorizedItem);
        }

        CategoryPreviewWindowText = CategoryGroups.Count == 0
            ? "No category preview is currently available."
            : $"Showing one category at a time. Each category preview shows up to {SelectedCategoryPreviewLimit} subjects.";

        UncategorizedPreviewWindowText = categorizationResult.UncategorizedSubjectCount switch
        {
            0 => "No uncategorized subjects in this preview.",
            <= UncategorizedPreviewLimit => $"Showing all {categorizationResult.UncategorizedSubjectCount} uncategorized subjects.",
            _ => $"Showing first {UncategorizedPreviewLimit} of {categorizationResult.UncategorizedSubjectCount} uncategorized subjects."
        };
    }

    private void RebuildSelectedCategorySubjects()
    {
        SelectedCategorySubjects.Clear();

        if (SelectedCategoryGroup is null)
        {
            OnPropertyChanged(nameof(HasSelectedCategory));
            return;
        }

        if (_categoryPreviewLookup.TryGetValue(SelectedCategoryGroup.CategoryKey, out var previewItems))
        {
            foreach (var previewItem in previewItems)
            {
                SelectedCategorySubjects.Add(previewItem);
            }
        }

        OnPropertyChanged(nameof(HasSelectedCategory));
    }

    private void ClearPreviewState()
    {
        CategoryCounts.Clear();
        CategoryGroups.Clear();
        SelectedCategorySubjects.Clear();
        UncategorizedSubjects.Clear();
        _categoryPreviewLookup.Clear();
        _lastBoundCategorizationResult = null;
        SelectedCategoryGroup = null;
        CategoryPreviewWindowText = "No category preview is currently available.";
        UncategorizedPreviewWindowText = "No uncategorized preview is currently available.";
    }

    private static string BuildCategoryPreviewSummary(int totalCount)
    {
        return totalCount <= SelectedCategoryPreviewLimit
            ? $"Showing all {totalCount} subjects in this category preview."
            : $"Showing first {SelectedCategoryPreviewLimit} of {totalCount} subjects in this category preview.";
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

    public sealed record CategoryGroupOptionItem(
        string DisplayName,
        string CategoryKey,
        int Count,
        string PreviewSummary)
    {
        public string OptionLabel => $"{DisplayName} ({Count})";
    }

    public sealed record CategorySubjectItem(
        string SubjectText,
        string Reason,
        string SourceMetadata);

    public sealed record UncategorizedItem(
        string SubjectText,
        string Reason,
        string SourceMetadata);
}
