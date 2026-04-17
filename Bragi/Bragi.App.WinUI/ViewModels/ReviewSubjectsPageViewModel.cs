using System;
using System.Collections.ObjectModel;
using System.Linq;
using Bragi.App.WinUI.ViewModels.Base;
using Bragi.Application.Workflow;
using Microsoft.Extensions.Logging;

namespace Bragi.App.WinUI.ViewModels;

public sealed class ReviewSubjectsPageViewModel : ObservableObject
{
    private const int PreviewLimit = 50;

    private readonly WizardSessionStore _wizardSessionStore;
    private readonly ILogger<ReviewSubjectsPageViewModel> _logger;

    private string _statusMessage = "Load input first to review extracted subjects.";
    private int _extractedSubjectCount;
    private int _duplicateCount;
    private int _blankOrIgnoredCount;
    private int _parseWarningCount;
    private bool _isReviewComplete;

    public ReviewSubjectsPageViewModel(
        WizardSessionStore wizardSessionStore,
        ILogger<ReviewSubjectsPageViewModel> logger)
    {
        _wizardSessionStore = wizardSessionStore ?? throw new ArgumentNullException(nameof(wizardSessionStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Subjects = new ObservableCollection<ReviewSubjectItem>();

        _wizardSessionStore.SessionChanged += OnSessionChanged;
        RefreshFromSession();
    }

    public ObservableCollection<ReviewSubjectItem> Subjects { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public int ExtractedSubjectCount
    {
        get => _extractedSubjectCount;
        private set => SetProperty(ref _extractedSubjectCount, value);
    }

    public int DuplicateCount
    {
        get => _duplicateCount;
        private set => SetProperty(ref _duplicateCount, value);
    }

    public int BlankOrIgnoredCount
    {
        get => _blankOrIgnoredCount;
        private set => SetProperty(ref _blankOrIgnoredCount, value);
    }

    public int ParseWarningCount
    {
        get => _parseWarningCount;
        private set => SetProperty(ref _parseWarningCount, value);
    }

    public bool IsReviewComplete
    {
        get => _isReviewComplete;
        private set => SetProperty(ref _isReviewComplete, value);
    }

    public bool HasExtraction => ExtractedSubjectCount > 0;

    public string PreviewWindowText =>
        HasExtraction
            ? $"Showing the first {Subjects.Count} extracted subjects."
            : "No extracted subjects are currently available.";

    public void MarkReviewComplete()
    {
        if (_wizardSessionStore.ExtractedSubjects is null)
        {
            StatusMessage = "Load input before marking subject review complete.";
            return;
        }

        _wizardSessionStore.MarkExtractionReviewComplete();
        RefreshFromSession();

        _logger.LogInformation("Extraction review marked complete.");
        StatusMessage = "Subject review marked complete. Continue to Preview Results.";
    }

    public void RefreshFromSession()
    {
        Subjects.Clear();

        var extractionResult = _wizardSessionStore.ExtractedSubjects;

        ExtractedSubjectCount = extractionResult?.ExtractedCount ?? 0;
        DuplicateCount = extractionResult?.DuplicateCount ?? 0;
        BlankOrIgnoredCount = extractionResult?.BlankOrIgnoredCount ?? 0;
        ParseWarningCount = extractionResult?.ParseWarningCount ?? 0;
        IsReviewComplete = _wizardSessionStore.State.IsExtractionReviewComplete;

        if (extractionResult is not null)
        {
            foreach (var subject in extractionResult.Subjects.Take(PreviewLimit))
            {
                Subjects.Add(new ReviewSubjectItem(
                    subject.SequenceNumber,
                    subject.OriginalSubject.Value,
                    subject.Entry.SourceRowNumber,
                    subject.Entry.SourceTitle,
                    subject.Entry.SourceRecordId));
            }

            if (!IsReviewComplete)
            {
                StatusMessage = "Review the extracted subjects, then mark the review complete.";
            }
        }

        OnPropertyChanged(nameof(HasExtraction));
        OnPropertyChanged(nameof(PreviewWindowText));
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        RefreshFromSession();
    }

    public sealed record ReviewSubjectItem(
        int SequenceNumber,
        string Subject,
        int? SourceRowNumber,
        string? SourceTitle,
        string? SourceRecordId)
    {
        public string SourceMetadata =>
            SourceRowNumber.HasValue
                ? $"Row {SourceRowNumber.Value}"
                : "No row metadata";

        public string OptionalMetadata =>
            !string.IsNullOrWhiteSpace(SourceRecordId)
                ? $"Record ID: {SourceRecordId}"
                : !string.IsNullOrWhiteSpace(SourceTitle)
                    ? SourceTitle!
                    : "No additional source metadata";
    }
}
