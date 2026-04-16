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
using Microsoft.Extensions.Logging;

namespace Bragi.App.WinUI.ViewModels;

public sealed class PreviewResultsPageViewModel : ObservableObject
{
    private readonly IWorkflowOrchestrator _workflowOrchestrator;
    private readonly WizardSessionStore _wizardSessionStore;
    private readonly BragiConfig _config;
    private readonly ILogger<PreviewResultsPageViewModel> _logger;

    private bool _isLoadingPreview;
    private string _statusMessage = "Complete subject review before generating the categorization preview.";
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
        CategoryCounts.Clear();
        CategoryGroups.Clear();
        UncategorizedSubjects.Clear();

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
            OnPropertyChanged(nameof(HasPreview));
            OnPropertyChanged(nameof(CanGeneratePreview));
            return;
        }

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
            var items = new ObservableCollection<CategorySubjectItem>(
                group.Select(item => new CategorySubjectItem(
                    item.SubjectText,
                    item.Reason,
                    item.SourceRowNumber.HasValue ? $"Row {item.SourceRowNumber.Value}" : "No row metadata")));

            CategoryGroups.Add(new CategoryGroupItem(
                GetDisplayName(displayNameLookup, group.Key),
                group.Key,
                items.Count,
                items));
        }

        foreach (var uncategorizedSubject in categorizationResult.UncategorizedSubjects
                     .OrderBy(item => item.Subject.SequenceNumber))
        {
            UncategorizedSubjects.Add(new UncategorizedItem(
                uncategorizedSubject.Subject.OriginalSubject.Value,
                uncategorizedSubject.Reason,
                uncategorizedSubject.Subject.Entry.SourceRowNumber.HasValue
                    ? $"Row {uncategorizedSubject.Subject.Entry.SourceRowNumber.Value}"
                    : "No row metadata"));
        }

        StatusMessage = "Preview results are ready.";
        OnPropertyChanged(nameof(HasPreview));
        OnPropertyChanged(nameof(CanGeneratePreview));
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
