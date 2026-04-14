using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Bragi.App.WinUI.Pages;
using Bragi.Application.Contracts;
using Bragi.Application.Workflow;

namespace Bragi.App.WinUI.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private static readonly StepDefinition[] StepDefinitions =
    [
        new(0, "Start", "Begin a new Bragi session and follow the guided workflow.", typeof(StartPage)),
        new(1, "Load Input", "Choose the source input file and prepare it for extraction.", typeof(LoadInputPage)),
        new(2, "Review Subjects", "Inspect extracted subjects before categorization.", typeof(ReviewSubjectsPage)),
        new(3, "Preview Results", "Review the categorization preview before export.", typeof(PreviewResultsPage)),
        new(4, "Export & Finish", "Generate export files and finalize the run.", typeof(ExportFinishPage))
    ];

    private readonly WizardSessionStore _wizardSessionStore;
    private readonly IStepNavigationService _stepNavigationService;

    private Type _currentPageType = typeof(StartPage);
    private string _currentStepTitle = "Start";
    private string _currentStepDescription = "Begin a new Bragi session and follow the guided workflow.";
    private string _busyStatusText = "Ready";
    private string _navigationStatusText = "Step 1 of 5";
    private bool _isBusy;

    public MainWindowViewModel(
        WizardSessionStore wizardSessionStore,
        IStepNavigationService stepNavigationService)
    {
        _wizardSessionStore = wizardSessionStore ?? throw new ArgumentNullException(nameof(wizardSessionStore));
        _stepNavigationService = stepNavigationService ?? throw new ArgumentNullException(nameof(stepNavigationService));

        WindowTitle = "Bragi";
        ShellTitle = "Bragi";
        ShellSubtitle = "Configurable subject categorization workflow";

        _wizardSessionStore.SessionChanged += OnWizardSessionChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string WindowTitle { get; }

    public string ShellTitle { get; }

    public string ShellSubtitle { get; }

    public Type CurrentPageType
    {
        get => _currentPageType;
        private set => SetProperty(ref _currentPageType, value);
    }

    public string CurrentStepTitle
    {
        get => _currentStepTitle;
        private set => SetProperty(ref _currentStepTitle, value);
    }

    public string CurrentStepDescription
    {
        get => _currentStepDescription;
        private set => SetProperty(ref _currentStepDescription, value);
    }

    public string BusyStatusText
    {
        get => _busyStatusText;
        private set => SetProperty(ref _busyStatusText, value);
    }

    public string NavigationStatusText
    {
        get => _navigationStatusText;
        private set => SetProperty(ref _navigationStatusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool CanCancelBusyOperation => _wizardSessionStore.State.IsBusy;

    public int CurrentStepIndex => _stepNavigationService.CurrentStepIndex;

    public bool CanMoveNext => FindNextUnlockedStepIndex(CurrentStepIndex).HasValue;

    public bool CanMovePrevious => FindPreviousUnlockedStepIndex(CurrentStepIndex).HasValue;

    public void Initialize()
    {
        _stepNavigationService.Initialize(
            StepDefinitions.Length,
            _wizardSessionStore.State.CurrentStepIndex);

        ApplyState(_wizardSessionStore.State);
    }

    public void MoveNext()
    {
        var nextStepIndex = FindNextUnlockedStepIndex(CurrentStepIndex);

        if (!nextStepIndex.HasValue)
        {
            return;
        }

        NavigateTo(nextStepIndex.Value);
    }

    public void MovePrevious()
    {
        var previousStepIndex = FindPreviousUnlockedStepIndex(CurrentStepIndex);

        if (!previousStepIndex.HasValue)
        {
            return;
        }

        NavigateTo(previousStepIndex.Value);
    }

    public void GoToStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= StepDefinitions.Length)
        {
            return;
        }

        if (_wizardSessionStore.State.IsStepLocked(stepIndex))
        {
            return;
        }

        NavigateTo(stepIndex);
    }

    public void CancelBusyOperation()
    {
        _wizardSessionStore.CancelBusyOperation();
    }

    public bool IsStepEnabled(int stepIndex)
    {
        return !_wizardSessionStore.State.IsStepLocked(stepIndex);
    }

    private void NavigateTo(int stepIndex)
    {
        _stepNavigationService.GoTo(stepIndex);
        _wizardSessionStore.SetCurrentStep(stepIndex);
    }

    private void OnWizardSessionChanged(object? sender, EventArgs e)
    {
        if (_stepNavigationService.TotalStepCount != StepDefinitions.Length)
        {
            _stepNavigationService.Initialize(
                StepDefinitions.Length,
                _wizardSessionStore.State.CurrentStepIndex);
        }
        else if (_stepNavigationService.CurrentStepIndex != _wizardSessionStore.State.CurrentStepIndex)
        {
            _stepNavigationService.GoTo(_wizardSessionStore.State.CurrentStepIndex);
        }

        ApplyState(_wizardSessionStore.State);
    }

    private void ApplyState(WizardState wizardState)
    {
        var currentStepDefinition = StepDefinitions[wizardState.CurrentStepIndex];

        CurrentPageType = currentStepDefinition.PageType;
        CurrentStepTitle = currentStepDefinition.Title;
        CurrentStepDescription = currentStepDefinition.Description;
        NavigationStatusText = $"Step {wizardState.CurrentStepIndex + 1} of {StepDefinitions.Length}";
        IsBusy = wizardState.IsBusy;
        BusyStatusText = wizardState.IsBusy
            ? "Busy - operation running. You can cancel safely."
            : "Ready";

        OnPropertyChanged(nameof(CurrentStepIndex));
        OnPropertyChanged(nameof(CanMoveNext));
        OnPropertyChanged(nameof(CanMovePrevious));
        OnPropertyChanged(nameof(CanCancelBusyOperation));
        OnPropertyChanged(nameof(IsStepEnabled));
    }

    private int? FindNextUnlockedStepIndex(int currentStepIndex)
    {
        for (var stepIndex = currentStepIndex + 1; stepIndex < StepDefinitions.Length; stepIndex++)
        {
            if (!_wizardSessionStore.State.IsStepLocked(stepIndex))
            {
                return stepIndex;
            }
        }

        return null;
    }

    private int? FindPreviousUnlockedStepIndex(int currentStepIndex)
    {
        for (var stepIndex = currentStepIndex - 1; stepIndex >= 0; stepIndex--)
        {
            if (!_wizardSessionStore.State.IsStepLocked(stepIndex))
            {
                return stepIndex;
            }
        }

        return null;
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed record StepDefinition(
        int StepIndex,
        string Title,
        string Description,
        Type PageType);
}
