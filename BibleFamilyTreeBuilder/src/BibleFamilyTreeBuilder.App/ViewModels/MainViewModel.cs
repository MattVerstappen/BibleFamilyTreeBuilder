using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using BibleFamilyTreeBuilder.App.Models;
using BibleFamilyTreeBuilder.App.Services;
using Microsoft.Win32;

namespace BibleFamilyTreeBuilder.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ProjectJsonService _projectJsonService = new();
    private readonly TreeLayoutService _treeLayoutService = new();
    private TreeProject _project = SampleProjectFactory.Create();
    private Person? _selectedPerson;
    private Person? _selectedSearchResult;
    private RelationshipListItem? _selectedRelationshipItem;
    private NameVariantListItem? _selectedNameVariantItem;
    private Person? _relationshipTargetPerson;
    private ParentKind _selectedParentKind = ParentKind.Biological;
    private string _searchText = "";
    private string _nameVariantNameUsedText = "";
    private string _nameVariantBookText = "";
    private string _nameVariantReferenceText = "";
    private string _nameVariantNotesText = "";
    private string _nameVariantTranslationOrSourceText = "";
    private string _relationshipDisplayLabelText = "";
    private string _relationshipBibleReferencesText = "";
    private string _relationshipNotesText = "";
    private ParentKind _selectedRelationshipParentKind = ParentKind.Unknown;
    private EvidenceLevel _selectedRelationshipEvidenceLevel = EvidenceLevel.Unknown;
    private double _zoom = 1.0;
    private LineageViewMode _lineageViewMode = LineageViewMode.FullTree;
    private string? _lineageRootPersonId;
    private string? _currentProjectFilePath;
    private bool _hasUnsavedChanges;

    public MainViewModel()
    {
        NewSampleTreeCommand = new RelayCommand(_ => NewSampleTree());
        AddPersonCommand = new RelayCommand(_ => AddPerson());
        DeletePersonCommand = new RelayCommand(_ => ConfirmAndDeleteSelectedPerson(Application.Current.MainWindow), _ => SelectedPerson is not null);
        AddChildCommand = new RelayCommand(_ => AddQuickPerson("New child", CardType.Default, RelationshipType.ParentChild, selectedPersonIsFrom: true, ParentKind.Biological, promptForOtherParent: true), _ => SelectedPerson is not null);
        AddParentCommand = new RelayCommand(_ => AddQuickPerson("New parent", CardType.Default, RelationshipType.ParentChild, selectedPersonIsFrom: false, ParentKind.Biological), _ => SelectedPerson is not null);
        AddSpouseCommand = new RelayCommand(_ => AddQuickPerson("New spouse", CardType.Default, RelationshipType.Marriage, selectedPersonIsFrom: true, ParentKind.Unknown), _ => SelectedPerson is not null);
        AddUnknownDescendantCommand = new RelayCommand(_ => AddQuickPerson("Unknown descendants", CardType.UnknownDescendant, RelationshipType.ParentChild, selectedPersonIsFrom: true, ParentKind.Unknown), _ => SelectedPerson is not null);
        AddGroupedPeopleCommand = new RelayCommand(_ => AddQuickPerson("Grouped people", CardType.GroupedPeople, RelationshipType.Marriage, selectedPersonIsFrom: true, ParentKind.Unknown, "Grouped people"), _ => SelectedPerson is not null);
        SaveProjectCommand = new RelayCommand(async _ => await SaveProjectAsync());
        SaveAsProjectCommand = new RelayCommand(async _ => await SaveProjectAsAsync());
        LoadProjectCommand = new RelayCommand(async _ => await LoadProjectAsync());
        CreateBackupCommand = new RelayCommand(async _ => await CreateManualBackupAsync());
        OpenBackupsFolderCommand = new RelayCommand(_ => OpenBackupsFolder());
        CheckTreeCommand = new RelayCommand(_ => ShowTreeCheckReport());
        ManageGenerationsCommand = new RelayCommand(_ => ManageGenerationsRequested?.Invoke());
        MergePeopleCommand = new RelayCommand(_ => MergePeopleRequested?.Invoke(), _ => Project.People.Count >= 2);
        ExportCurrentViewCommand = new RelayCommand(_ => ExportCurrentViewRequested?.Invoke());
        ExportFullTreeCommand = new RelayCommand(_ => ExportFullTreeRequested?.Invoke());
        ViewAncestorsCommand = new RelayCommand(_ => SetLineageView(LineageViewMode.Ancestors), _ => SelectedPerson is not null);
        ViewDescendantsCommand = new RelayCommand(_ => SetLineageView(LineageViewMode.Descendants), _ => SelectedPerson is not null);
        ViewLineageCommand = new RelayCommand(_ => SetLineageView(LineageViewMode.AncestorsAndDescendants), _ => SelectedPerson is not null);
        ReturnToFullTreeCommand = new RelayCommand(_ => ReturnToFullTree());
        AutoLayoutCommand = new RelayCommand(_ => RequestCanvasRefresh());
        ZoomInCommand = new RelayCommand(_ => Zoom = Math.Min(2.5, Zoom + 0.1));
        ZoomOutCommand = new RelayCommand(_ => Zoom = Math.Max(0.35, Zoom - 0.1));
        CenterSelectedCommand = new RelayCommand(_ => CenterSelectedRequested?.Invoke(), _ => SelectedPerson is not null);
        FitTreeToViewCommand = new RelayCommand(_ => FitTreeToViewRequested?.Invoke());
        ResetViewCommand = new RelayCommand(_ =>
        {
            Zoom = 1.0;
            ResetCanvasViewRequested?.Invoke();
        });
        NudgeUpCommand = new RelayCommand(_ => NudgeSelectedPerson(0, -20), _ => SelectedPerson is not null);
        NudgeDownCommand = new RelayCommand(_ => NudgeSelectedPerson(0, 20), _ => SelectedPerson is not null);
        NudgeLeftCommand = new RelayCommand(_ => NudgeSelectedPerson(-20, 0), _ => SelectedPerson is not null);
        NudgeRightCommand = new RelayCommand(_ => NudgeSelectedPerson(20, 0), _ => SelectedPerson is not null);
        ClearManualPositionCommand = new RelayCommand(_ => ClearManualPosition(), _ => SelectedPerson is not null);
        AddParentChildRelationshipCommand = new RelayCommand(_ => AddParentChildRelationship(), _ => CanAddRelationship);
        AddMarriageRelationshipCommand = new RelayCommand(_ => AddMarriageRelationship(), _ => CanAddRelationship);
        SaveRelationshipChangesCommand = new RelayCommand(_ => SaveSelectedRelationshipChanges(), _ => SelectedRelationshipItem is not null);
        DeleteRelationshipCommand = new RelayCommand(_ => ConfirmAndDeleteSelectedRelationship(Application.Current.MainWindow), _ => SelectedRelationshipItem is not null);
        ClearGenerationLabelCommand = new RelayCommand(_ => ClearSelectedGenerationLabel(), _ => GetSelectedEffectiveGeneration().HasValue);
        SetGenerationToLatestCommand = new RelayCommand(_ => SetGenerationToLatest(), _ => SelectedPerson is not null);
        AddNameVariantCommand = new RelayCommand(_ => AddNameVariant(), _ => SelectedPerson is not null);
        SaveNameVariantCommand = new RelayCommand(_ => SaveSelectedNameVariant(), _ => SelectedNameVariantItem is not null);
        DeleteNameVariantCommand = new RelayCommand(_ => ConfirmAndDeleteSelectedNameVariant(), _ => SelectedNameVariantItem is not null);
        ApplyThemeCommand = new RelayCommand(parameter => Theming.ThemeManager.ApplyByName(parameter as string ?? ""));
        CustomizeThemeCommand = new RelayCommand(_ => CustomizeThemeRequested?.Invoke());
        ExitCommand = new RelayCommand(_ => Application.Current.MainWindow?.Close());

        AttachProjectEvents();
        RebuildSearchResults();
        SelectedPerson = Project.People.FirstOrDefault();
        RelationshipTargetPerson = Project.People.Skip(1).FirstOrDefault();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? CanvasRefreshRequested;
    public event Action? ResetCanvasViewRequested;
    public event Action? CenterSelectedRequested;
    public event Action? FitTreeToViewRequested;
    public event Action? ExportCurrentViewRequested;
    public event Action? ExportFullTreeRequested;
    public event Action? ManageGenerationsRequested;
    public event Action? MergePeopleRequested;
    public event Action? CustomizeThemeRequested;

    public TreeProject Project
    {
        get => _project;
        private set
        {
            if (_project == value)
            {
                return;
            }

            DetachProjectEvents();
            _project = value;
            AttachProjectEvents();
            OnPropertyChanged();
            OnPropertyChanged(nameof(People));
            OnPropertyChanged(nameof(Relationships));
            OnPropertyChanged(nameof(DisplayProject));
            OnPropertyChanged(nameof(CurrentViewModeText));
            NotifyGenerationLabelProperties();
            RebuildSearchResults();
            RequestCanvasRefresh();
        }
    }

    public TreeProject DisplayProject => BuildDisplayProject();
    public ObservableCollection<Person> People => Project.People;
    public ObservableCollection<Relationship> Relationships => Project.Relationships;
    public ObservableCollection<Person> SearchResults { get; } = [];
    public ObservableCollection<RelationshipListItem> SelectedPersonRelationships { get; } = [];
    public ObservableCollection<NameVariantListItem> SelectedPersonNameVariants { get; } = [];
    public Array CardTypes => Enum.GetValues(typeof(CardType));
    public Array ParentKinds => Enum.GetValues(typeof(ParentKind));
    public Array EvidenceLevels => Enum.GetValues(typeof(EvidenceLevel));

    public string? CurrentProjectFilePath
    {
        get => _currentProjectFilePath;
        private set
        {
            if (_currentProjectFilePath == value)
            {
                return;
            }

            _currentProjectFilePath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(WindowTitle));
        }
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set
        {
            if (_hasUnsavedChanges == value)
            {
                return;
            }

            _hasUnsavedChanges = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(WindowTitle));
        }
    }

    public string WindowTitle
    {
        get
        {
            var fileName = string.IsNullOrWhiteSpace(CurrentProjectFilePath)
                ? ""
                : $" - {Path.GetFileName(CurrentProjectFilePath)}";
            var unsavedIndicator = HasUnsavedChanges ? " *" : "";
            return $"Bible Family Tree Builder{fileName}{unsavedIndicator}";
        }
    }

    public string SelectedEffectiveGenerationText
    {
        get
        {
            var generation = GetSelectedEffectiveGeneration();
            return generation.HasValue ? $"Generation {generation.Value}" : "No person selected";
        }
    }

    public string SelectedGenerationOverrideStatusText
    {
        get
        {
            if (SelectedPerson is null)
            {
                return "No person selected";
            }

            return SelectedPerson.GenerationOverride.HasValue
                ? $"Manual generation override active: {SelectedPerson.GenerationOverride.Value}"
                : "Using automatic generation";
        }
    }

    public string SelectedGenerationLabelTitle
    {
        get => GetSelectedGenerationLabel()?.Title ?? "";
        set => UpdateSelectedGenerationLabel(title: value, notes: null);
    }

    public string SelectedGenerationLabelNotes
    {
        get => GetSelectedGenerationLabel()?.Notes ?? "";
        set => UpdateSelectedGenerationLabel(title: null, notes: value);
    }

    public Person? SelectedPerson
    {
        get => _selectedPerson;
        set
        {
            if (_selectedPerson == value)
            {
                return;
            }

            if (value is not null && _lineageViewMode != LineageViewMode.FullTree && !IsPersonVisibleInCurrentView(value.Id))
            {
                ReturnToFullTree();
            }

            _selectedPerson = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedAlsoKnownAsText));
            OnPropertyChanged(nameof(SelectedBibleReferencesText));
            OnPropertyChanged(nameof(SelectedGenerationOverrideText));
            OnPropertyChanged(nameof(SelectedEffectiveGenerationText));
            OnPropertyChanged(nameof(SelectedGenerationOverrideStatusText));
            OnPropertyChanged(nameof(SelectedGenerationLabelTitle));
            OnPropertyChanged(nameof(SelectedGenerationLabelNotes));
            OnPropertyChanged(nameof(SelectedPersonRelationshipSummary));
            OnPropertyChanged(nameof(SelectedPersonNameVariantSummary));
            OnPropertyChanged(nameof(HasSelectedPerson));
            RebuildSelectedPersonRelationships();
            RebuildSelectedPersonNameVariants();
            ClearNameVariantEditor();
            SyncSelectedSearchResult();
            CommandManager.InvalidateRequerySuggested();
            RefreshDisplayProject();
            RequestCanvasRefresh();
        }
    }

    public bool HasSelectedPerson => SelectedPerson is not null;

    public RelationshipListItem? SelectedRelationshipItem
    {
        get => _selectedRelationshipItem;
        set
        {
            _selectedRelationshipItem = value;
            OnPropertyChanged();
            LoadRelationshipEditor(value?.Relationship);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public NameVariantListItem? SelectedNameVariantItem
    {
        get => _selectedNameVariantItem;
        set
        {
            _selectedNameVariantItem = value;
            OnPropertyChanged();
            LoadNameVariantEditor(value?.Variant);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public Person? SelectedSearchResult
    {
        get => _selectedSearchResult;
        set
        {
            _selectedSearchResult = value;
            OnPropertyChanged();

            if (value is not null)
            {
                if (_lineageViewMode != LineageViewMode.FullTree && !IsPersonVisibleInCurrentView(value.Id))
                {
                    ReturnToFullTree();
                }

                SelectedPerson = value;
            }
        }
    }

    public Person? RelationshipTargetPerson
    {
        get => _relationshipTargetPerson;
        set
        {
            _relationshipTargetPerson = value;
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public ParentKind SelectedParentKind
    {
        get => _selectedParentKind;
        set
        {
            _selectedParentKind = value;
            OnPropertyChanged();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value)
            {
                return;
            }

            _searchText = value;
            OnPropertyChanged();
            RebuildSearchResults();
        }
    }

    public double Zoom
    {
        get => _zoom;
        set
        {
            if (Math.Abs(_zoom - value) < 0.001)
            {
                return;
            }

            _zoom = value;
            OnPropertyChanged();
        }
    }

    public string CurrentViewModeText
    {
        get
        {
            var rootName = Project.People.FirstOrDefault(person => person.Id == _lineageRootPersonId)?.EffectiveDisplayName ?? "selected person";
            return _lineageViewMode switch
            {
                LineageViewMode.Ancestors => $"Ancestors of {rootName}",
                LineageViewMode.Descendants => $"Descendants of {rootName}",
                LineageViewMode.AncestorsAndDescendants => $"Lineage of {rootName}",
                _ => "Full Tree"
            };
        }
    }

    public string SelectedPersonRelationshipSummary
    {
        get
        {
            if (SelectedPerson is null)
            {
                return "No person selected.";
            }

            var parents = Project.Relationships
                .Where(r => r.Type == RelationshipType.ParentChild && r.ToPersonId == SelectedPerson.Id)
                .Select(r => FormatRelatedPerson(r.FromPersonId, r.ParentKind.ToString()))
                .ToList();

            var spouses = Project.Relationships
                .Where(r => r.Type == RelationshipType.Marriage && (r.FromPersonId == SelectedPerson.Id || r.ToPersonId == SelectedPerson.Id))
                .Select(r => FormatRelatedPerson(r.FromPersonId == SelectedPerson.Id ? r.ToPersonId : r.FromPersonId, "Marriage"))
                .ToList();

            var children = Project.Relationships
                .Where(r => r.Type == RelationshipType.ParentChild && r.FromPersonId == SelectedPerson.Id)
                .Select(r => FormatRelatedPerson(r.ToPersonId, r.ParentKind.ToString()))
                .ToList();

            return
                $"Parents:{Environment.NewLine}{FormatSummaryList(parents)}{Environment.NewLine}{Environment.NewLine}" +
                $"Spouses:{Environment.NewLine}{FormatSummaryList(spouses)}{Environment.NewLine}{Environment.NewLine}" +
                $"Children:{Environment.NewLine}{FormatSummaryList(children)}";
        }
    }

    public string SelectedPersonNameVariantSummary
    {
        get
        {
            if (SelectedPerson is null)
            {
                return "No person selected.";
            }

            if (SelectedPerson.NameVariants.Count == 0)
            {
                return "Name Variants:" + Environment.NewLine + "- None";
            }

            var lines = SelectedPerson.NameVariants
                .Select(FormatNameVariantSummaryLine)
                .ToList();

            return "Name Variants:" + Environment.NewLine + string.Join(Environment.NewLine, lines);
        }
    }

    public string SelectedAlsoKnownAsText
    {
        get => SelectedPerson is null ? "" : string.Join(Environment.NewLine, SelectedPerson.AlsoKnownAs);
        set
        {
            if (SelectedPerson is null)
            {
                return;
            }

            SelectedPerson.AlsoKnownAs = SplitLines(value);
            OnPropertyChanged();
            MarkProjectDirty();
            RequestCanvasRefresh();
        }
    }

    public string SelectedBibleReferencesText
    {
        get => SelectedPerson is null ? "" : string.Join(Environment.NewLine, SelectedPerson.BibleReferences);
        set
        {
            if (SelectedPerson is null)
            {
                return;
            }

            SelectedPerson.BibleReferences = SplitLines(value);
            OnPropertyChanged();
            MarkProjectDirty();
        }
    }

    public string SelectedGenerationOverrideText
    {
        get => SelectedPerson?.GenerationOverride?.ToString() ?? "";
        set
        {
            if (SelectedPerson is null)
            {
                return;
            }

            SelectedPerson.GenerationOverride = int.TryParse(value, out var generation) ? generation : null;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedEffectiveGenerationText));
            OnPropertyChanged(nameof(SelectedGenerationOverrideStatusText));
            OnPropertyChanged(nameof(SelectedGenerationLabelTitle));
            OnPropertyChanged(nameof(SelectedGenerationLabelNotes));
            MarkProjectDirty();
            RefreshDisplayProject();
            RequestCanvasRefresh();
        }
    }

    public string NameVariantNameUsedText
    {
        get => _nameVariantNameUsedText;
        set
        {
            _nameVariantNameUsedText = value;
            OnPropertyChanged();
        }
    }

    public string NameVariantBookText
    {
        get => _nameVariantBookText;
        set
        {
            _nameVariantBookText = value;
            OnPropertyChanged();
        }
    }

    public string NameVariantReferenceText
    {
        get => _nameVariantReferenceText;
        set
        {
            _nameVariantReferenceText = value;
            OnPropertyChanged();
        }
    }

    public string NameVariantNotesText
    {
        get => _nameVariantNotesText;
        set
        {
            _nameVariantNotesText = value;
            OnPropertyChanged();
        }
    }

    public string NameVariantTranslationOrSourceText
    {
        get => _nameVariantTranslationOrSourceText;
        set
        {
            _nameVariantTranslationOrSourceText = value;
            OnPropertyChanged();
        }
    }

    public string RelationshipDisplayLabelText
    {
        get => _relationshipDisplayLabelText;
        set
        {
            _relationshipDisplayLabelText = value;
            OnPropertyChanged();
        }
    }

    public ParentKind SelectedRelationshipParentKind
    {
        get => _selectedRelationshipParentKind;
        set
        {
            _selectedRelationshipParentKind = value;
            OnPropertyChanged();
        }
    }

    public EvidenceLevel SelectedRelationshipEvidenceLevel
    {
        get => _selectedRelationshipEvidenceLevel;
        set
        {
            _selectedRelationshipEvidenceLevel = value;
            OnPropertyChanged();
        }
    }

    public string RelationshipBibleReferencesText
    {
        get => _relationshipBibleReferencesText;
        set
        {
            _relationshipBibleReferencesText = value;
            OnPropertyChanged();
        }
    }

    public string RelationshipNotesText
    {
        get => _relationshipNotesText;
        set
        {
            _relationshipNotesText = value;
            OnPropertyChanged();
        }
    }

    public ICommand NewSampleTreeCommand { get; }
    public ICommand AddPersonCommand { get; }
    public ICommand DeletePersonCommand { get; }
    public ICommand AddChildCommand { get; }
    public ICommand AddParentCommand { get; }
    public ICommand AddSpouseCommand { get; }
    public ICommand AddUnknownDescendantCommand { get; }
    public ICommand AddGroupedPeopleCommand { get; }
    public ICommand SaveProjectCommand { get; }
    public ICommand SaveAsProjectCommand { get; }
    public ICommand LoadProjectCommand { get; }
    public ICommand CreateBackupCommand { get; }
    public ICommand OpenBackupsFolderCommand { get; }
    public ICommand CheckTreeCommand { get; }
    public ICommand ManageGenerationsCommand { get; }
    public ICommand MergePeopleCommand { get; }
    public ICommand ExportCurrentViewCommand { get; }
    public ICommand ExportFullTreeCommand { get; }
    public ICommand ViewAncestorsCommand { get; }
    public ICommand ViewDescendantsCommand { get; }
    public ICommand ViewLineageCommand { get; }
    public ICommand ReturnToFullTreeCommand { get; }
    public ICommand AutoLayoutCommand { get; }
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand CenterSelectedCommand { get; }
    public ICommand FitTreeToViewCommand { get; }
    public ICommand ResetViewCommand { get; }
    public ICommand NudgeUpCommand { get; }
    public ICommand NudgeDownCommand { get; }
    public ICommand NudgeLeftCommand { get; }
    public ICommand NudgeRightCommand { get; }
    public ICommand ClearManualPositionCommand { get; }
    public ICommand AddParentChildRelationshipCommand { get; }
    public ICommand AddMarriageRelationshipCommand { get; }
    public ICommand SaveRelationshipChangesCommand { get; }
    public ICommand DeleteRelationshipCommand { get; }
    public ICommand ClearGenerationLabelCommand { get; }
    public ICommand SetGenerationToLatestCommand { get; }
    public ICommand AddNameVariantCommand { get; }
    public ICommand SaveNameVariantCommand { get; }
    public ICommand DeleteNameVariantCommand { get; }
    public ICommand ApplyThemeCommand { get; }
    public ICommand CustomizeThemeCommand { get; }
    public ICommand ExitCommand { get; }

    public void NotifyGenerationLabelsChanged()
    {
        MarkProjectDirty();
        NotifyGenerationLabelProperties();
        RefreshDisplayProject();
        RequestCanvasRefresh();
    }

    private bool CanAddRelationship =>
        SelectedPerson is not null &&
        RelationshipTargetPerson is not null &&
        SelectedPerson.Id != RelationshipTargetPerson.Id;

    private TreeProject BuildDisplayProject()
    {
        if (_lineageViewMode == LineageViewMode.FullTree ||
            string.IsNullOrWhiteSpace(_lineageRootPersonId) ||
            Project.People.All(person => person.Id != _lineageRootPersonId))
        {
            return Project;
        }

        var coreLineageIds = GetCoreLineagePersonIds(_lineageRootPersonId, _lineageViewMode);
        var visibleIds = coreLineageIds.ToHashSet();
        var visibleRelationships = Project.Relationships
            .Where(relationship => relationship.Type == RelationshipType.ParentChild &&
                                   coreLineageIds.Contains(relationship.FromPersonId) &&
                                   coreLineageIds.Contains(relationship.ToPersonId))
            .ToList();

        foreach (var marriage in Project.Relationships.Where(relationship => relationship.Type == RelationshipType.Marriage))
        {
            if (!coreLineageIds.Contains(marriage.FromPersonId) && !coreLineageIds.Contains(marriage.ToPersonId))
            {
                continue;
            }

            visibleIds.Add(marriage.FromPersonId);
            visibleIds.Add(marriage.ToPersonId);
            visibleRelationships.Add(marriage);
        }

        return new TreeProject
        {
            Name = $"{Project.Name} - {CurrentViewModeText}",
            People = new ObservableCollection<Person>(Project.People.Where(person => visibleIds.Contains(person.Id))),
            Relationships = new ObservableCollection<Relationship>(visibleRelationships),
            GenerationLabels = new ObservableCollection<GenerationLabel>(Project.GenerationLabels)
        };
    }

    private HashSet<string> GetCoreLineagePersonIds(string rootPersonId, LineageViewMode mode)
    {
        var ids = new HashSet<string> { rootPersonId };

        if (mode is LineageViewMode.Ancestors or LineageViewMode.AncestorsAndDescendants)
        {
            AddAncestors(rootPersonId, ids);
        }

        if (mode is LineageViewMode.Descendants or LineageViewMode.AncestorsAndDescendants)
        {
            AddDescendants(rootPersonId, ids);
        }

        return ids;
    }

    private void AddAncestors(string personId, HashSet<string> ids)
    {
        var parentIds = Project.Relationships
            .Where(relationship => relationship.Type == RelationshipType.ParentChild && relationship.ToPersonId == personId)
            .Select(relationship => relationship.FromPersonId)
            .Where(parentId => !string.IsNullOrWhiteSpace(parentId))
            .ToList();

        foreach (var parentId in parentIds)
        {
            if (ids.Add(parentId))
            {
                AddAncestors(parentId, ids);
            }
        }
    }

    private void AddDescendants(string personId, HashSet<string> ids)
    {
        var childIds = Project.Relationships
            .Where(relationship => relationship.Type == RelationshipType.ParentChild && relationship.FromPersonId == personId)
            .Select(relationship => relationship.ToPersonId)
            .Where(childId => !string.IsNullOrWhiteSpace(childId))
            .ToList();

        foreach (var childId in childIds)
        {
            if (ids.Add(childId))
            {
                AddDescendants(childId, ids);
            }
        }
    }

    private bool IsPersonVisibleInCurrentView(string personId)
    {
        return _lineageViewMode == LineageViewMode.FullTree ||
               BuildDisplayProject().People.Any(person => person.Id == personId);
    }

    private void SetLineageView(LineageViewMode mode)
    {
        if (SelectedPerson is null)
        {
            ShowFriendlyMessage("Select a person before choosing a lineage view.", "No person selected");
            return;
        }

        _lineageViewMode = mode;
        _lineageRootPersonId = SelectedPerson.Id;
        RefreshDisplayProject();
    }

    private void ReturnToFullTree()
    {
        _lineageViewMode = LineageViewMode.FullTree;
        _lineageRootPersonId = null;
        RefreshDisplayProject();
    }

    private void RefreshDisplayProject()
    {
        OnPropertyChanged(nameof(DisplayProject));
        OnPropertyChanged(nameof(CurrentViewModeText));
        CommandManager.InvalidateRequerySuggested();
    }

    private void NewSampleTree()
    {
        if (!ConfirmContinueWithoutSaving(Application.Current.MainWindow))
        {
            return;
        }

        ReturnToFullTree();
        Project = SampleProjectFactory.Create();
        CurrentProjectFilePath = null;
        HasUnsavedChanges = true;
        SearchText = "";
        SelectedSearchResult = null;
        SelectedPerson = Project.People.FirstOrDefault();
        RelationshipTargetPerson = Project.People.Skip(1).FirstOrDefault();
    }

    private void AddPerson()
    {
        var person = CreatePersonWithDuplicateWarning("New person", CardType.Default);
        if (person is null)
        {
            return;
        }

        Project.People.Add(person);
        SelectedPerson = person;
    }

    private void AddQuickPerson(
        string name,
        CardType cardType,
        RelationshipType relationshipType,
        bool selectedPersonIsFrom,
        ParentKind parentKind,
        string label = "",
        bool promptForOtherParent = false)
    {
        if (SelectedPerson is null)
        {
            ShowFriendlyMessage("Select a person first, then use the quick add buttons.", "No person selected");
            return;
        }

        var originalSelectedPerson = SelectedPerson;
        var newPerson = CreatePersonWithDuplicateWarning(name, cardType);
        if (newPerson is null)
        {
            return;
        }

        Project.People.Add(newPerson);

        var fromPersonId = selectedPersonIsFrom ? originalSelectedPerson.Id : newPerson.Id;
        var toPersonId = selectedPersonIsFrom ? newPerson.Id : originalSelectedPerson.Id;

        if (!TryAddRelationship(relationshipType, fromPersonId, toPersonId, parentKind, label))
        {
            ShowFriendlyMessage(
                $"{newPerson.EffectiveDisplayName} was added, but the relationship could not be created. You can connect it manually.",
                "Quick add");
        }
        else if (promptForOtherParent && relationshipType == RelationshipType.ParentChild && selectedPersonIsFrom)
        {
            OfferSecondParent(originalSelectedPerson, newPerson);
        }

        SelectedPerson = newPerson;
        RelationshipTargetPerson = originalSelectedPerson;
    }

    private void OfferSecondParent(Person firstParent, Person child)
    {
        var spouses = Project.Relationships
            .Where(relationship => relationship.Type == RelationshipType.Marriage &&
                                   (relationship.FromPersonId == firstParent.Id || relationship.ToPersonId == firstParent.Id))
            .Select(relationship => relationship.FromPersonId == firstParent.Id ? relationship.ToPersonId : relationship.FromPersonId)
            .Distinct()
            .Select(spouseId => Project.People.FirstOrDefault(person => person.Id == spouseId))
            .OfType<Person>()
            .Where(spouse => spouse.Id != child.Id)
            .ToList();

        if (spouses.Count == 0)
        {
            return;
        }

        Person? otherParent;
        if (spouses.Count == 1)
        {
            var result = MessageBox.Show(
                Application.Current.MainWindow,
                $"Also add {spouses[0].EffectiveDisplayName} as the other parent of {child.EffectiveDisplayName}?",
                "Second parent",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            otherParent = result == MessageBoxResult.Yes ? spouses[0] : null;
        }
        else
        {
            otherParent = SelectPersonDialog.Show(
                Application.Current.MainWindow,
                "Choose second parent",
                $"{firstParent.EffectiveDisplayName} has more than one spouse. Who is the other parent of {child.EffectiveDisplayName}?",
                spouses);
        }

        if (otherParent is null)
        {
            return;
        }

        Project.Relationships.Add(new Relationship
        {
            Type = RelationshipType.ParentChild,
            FromPersonId = otherParent.Id,
            ToPersonId = child.Id,
            ParentKind = ParentKind.Biological,
            Label = ParentKind.Biological.ToString()
        });
    }

    private void SetGenerationToLatest()
    {
        if (SelectedPerson is null)
        {
            return;
        }

        var layout = _treeLayoutService.Layout(Project);
        var latestGeneration = layout.PersonGenerations.Values.DefaultIfEmpty(0).Max();
        SelectedGenerationOverrideText = latestGeneration.ToString();
    }

    public void ConfirmAndDeleteSelectedPerson(Window? owner)
    {
        if (SelectedPerson is null)
        {
            return;
        }

        var message = GetDeleteConfirmationMessage();
        var result = owner is null
            ? MessageBox.Show(message, "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            : MessageBox.Show(owner, message, "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            if (!TryCreateAutoBackupBeforeDestructiveAction(owner, "deleting a person"))
            {
                return;
            }

            DeleteSelectedPerson();
        }
    }

    public string GetDeleteConfirmationMessage()
    {
        if (SelectedPerson is null)
        {
            return "No person is selected.";
        }

        var relationshipCount = Project.Relationships
            .Count(relationship => relationship.FromPersonId == SelectedPerson.Id || relationship.ToPersonId == SelectedPerson.Id);

        return relationshipCount == 0
            ? $"Delete {SelectedPerson.EffectiveDisplayName}?"
            : $"Delete {SelectedPerson.EffectiveDisplayName} and remove {relationshipCount} connected relationship(s)?";
    }

    private void DeleteSelectedPerson()
    {
        if (SelectedPerson is null)
        {
            return;
        }

        var personToDelete = SelectedPerson;
        var relationshipsToDelete = Project.Relationships
            .Where(relationship => relationship.FromPersonId == personToDelete.Id || relationship.ToPersonId == personToDelete.Id)
            .ToList();

        foreach (var relationship in relationshipsToDelete)
        {
            Project.Relationships.Remove(relationship);
        }

        Project.People.Remove(personToDelete);
        SelectedPerson = Project.People.FirstOrDefault();
        RelationshipTargetPerson = Project.People.FirstOrDefault(person => person.Id != SelectedPerson?.Id);
    }

    public string BuildMergePreview(Person? primaryPerson, Person? secondaryPerson)
    {
        if (primaryPerson is null || secondaryPerson is null)
        {
            return "Choose both a primary person and a secondary person.";
        }

        if (primaryPerson.Id == secondaryPerson.Id)
        {
            return "Choose two different people. A person cannot be merged with themselves.";
        }

        if (!Project.People.Any(person => person.Id == primaryPerson.Id) ||
            !Project.People.Any(person => person.Id == secondaryPerson.Id))
        {
            return "Both people must still exist in the project before they can be merged.";
        }

        var relationshipsToMove = Project.Relationships
            .Where(relationship => relationship.FromPersonId == secondaryPerson.Id || relationship.ToPersonId == secondaryPerson.Id)
            .Select(relationship => DescribeRelationshipMove(relationship, primaryPerson, secondaryPerson))
            .ToList();

        var aliasesToAdd = GetAliasesToAdd(primaryPerson, secondaryPerson);
        var bibleReferencesToAdd = GetDistinctValuesToAdd(primaryPerson.BibleReferences, secondaryPerson.BibleReferences);
        var nameVariantsToAdd = secondaryPerson.NameVariants
            .Where(variant => !primaryPerson.NameVariants.Any(existing => NameVariantsMatch(existing, variant)))
            .Select(FormatNameVariantListText)
            .ToList();

        var cardTypeNote = primaryPerson.CardType != CardType.JesusLine && secondaryPerson.CardType == CardType.JesusLine
            ? "Primary will be changed to JesusLine so the Jesus Line marker is not lost."
            : "Primary card type will be kept.";

        var generationNote = GetMergeGenerationPreview(primaryPerson, secondaryPerson);
        var manualOffsetNote = GetMergeManualOffsetPreview(primaryPerson, secondaryPerson);
        var notesNote = GetMergeNotesPreview(primaryPerson, secondaryPerson);

        return
            $"Primary person to keep:{Environment.NewLine}- {primaryPerson.EffectiveDisplayName}{Environment.NewLine}{Environment.NewLine}" +
            $"Secondary person to remove after merge:{Environment.NewLine}- {secondaryPerson.EffectiveDisplayName}{Environment.NewLine}{Environment.NewLine}" +
            $"Relationships that will be moved:{Environment.NewLine}{FormatReportList(relationshipsToMove)}{Environment.NewLine}{Environment.NewLine}" +
            $"AlsoKnownAs values that will be combined:{Environment.NewLine}{FormatReportList(aliasesToAdd)}{Environment.NewLine}{Environment.NewLine}" +
            $"Bible references that will be combined:{Environment.NewLine}{FormatReportList(bibleReferencesToAdd)}{Environment.NewLine}{Environment.NewLine}" +
            $"Name variants that will be combined:{Environment.NewLine}{FormatReportList(nameVariantsToAdd)}{Environment.NewLine}{Environment.NewLine}" +
            $"Notes:{Environment.NewLine}- {notesNote}{Environment.NewLine}{Environment.NewLine}" +
            $"Card type:{Environment.NewLine}- {cardTypeNote}{Environment.NewLine}{Environment.NewLine}" +
            $"GenerationOverride:{Environment.NewLine}- {generationNote}{Environment.NewLine}{Environment.NewLine}" +
            $"Manual offsets:{Environment.NewLine}- {manualOffsetNote}";
    }

    public bool TryMergePeople(Person? primaryPerson, Person? secondaryPerson, out string message, Window? owner = null)
    {
        message = "";

        if (primaryPerson is null || secondaryPerson is null)
        {
            message = "Choose both a primary person and a secondary person before merging.";
            return false;
        }

        var primary = Project.People.FirstOrDefault(person => person.Id == primaryPerson.Id);
        var secondary = Project.People.FirstOrDefault(person => person.Id == secondaryPerson.Id);

        if (primary is null || secondary is null)
        {
            message = "Both people must still exist in the project before they can be merged.";
            return false;
        }

        if (primary.Id == secondary.Id)
        {
            message = "Choose two different people. A person cannot be merged with themselves.";
            return false;
        }

        if (!TryCreateAutoBackupBeforeDestructiveAction(owner, "merging people"))
        {
            message = "Merge was canceled because the automatic backup step was not completed.";
            return false;
        }

        var mergeNotes = new List<string>();

        foreach (var alias in GetAliasesToAdd(primary, secondary))
        {
            AddDistinct(primary.AlsoKnownAs, alias);
        }

        foreach (var reference in secondary.BibleReferences)
        {
            AddDistinct(primary.BibleReferences, reference);
        }

        foreach (var variant in secondary.NameVariants)
        {
            if (primary.NameVariants.Any(existing => NameVariantsMatch(existing, variant)))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(variant.Id) || primary.NameVariants.Any(existing => existing.Id == variant.Id))
            {
                variant.Id = Guid.NewGuid().ToString("N");
            }

            primary.NameVariants.Add(variant);
        }

        MergeNotes(primary, secondary);

        if (primary.CardType != CardType.JesusLine && secondary.CardType == CardType.JesusLine)
        {
            primary.CardType = CardType.JesusLine;
            mergeNotes.Add("Primary card type was changed to JesusLine because the secondary person was marked JesusLine.");
        }

        if (!primary.GenerationOverride.HasValue && secondary.GenerationOverride.HasValue)
        {
            primary.GenerationOverride = secondary.GenerationOverride;
            mergeNotes.Add("Primary copied the secondary person's GenerationOverride.");
        }
        else if (primary.GenerationOverride.HasValue && secondary.GenerationOverride.HasValue)
        {
            mergeNotes.Add("Both people had GenerationOverride values, so the primary person's value was kept.");
        }

        if (!HasManualOffset(primary) && HasManualOffset(secondary))
        {
            primary.ManualXOffset = secondary.ManualXOffset;
            primary.ManualYOffset = secondary.ManualYOffset;
            mergeNotes.Add("Primary copied the secondary person's manual nudge offsets.");
        }
        else if (HasManualOffset(primary) && HasManualOffset(secondary))
        {
            mergeNotes.Add("Both people had manual nudge offsets, so the primary person's offsets were kept.");
        }

        var movedRelationshipCount = 0;
        var removedRelationshipCount = 0;
        foreach (var relationship in Project.Relationships.ToList())
        {
            if (relationship.FromPersonId != secondary.Id && relationship.ToPersonId != secondary.Id)
            {
                continue;
            }

            var newFromPersonId = relationship.FromPersonId == secondary.Id ? primary.Id : relationship.FromPersonId;
            var newToPersonId = relationship.ToPersonId == secondary.Id ? primary.Id : relationship.ToPersonId;

            if (newFromPersonId == newToPersonId)
            {
                Project.Relationships.Remove(relationship);
                removedRelationshipCount++;
                continue;
            }

            if (RelationshipDuplicateExistsAfterMerge(relationship, newFromPersonId, newToPersonId))
            {
                Project.Relationships.Remove(relationship);
                removedRelationshipCount++;
                continue;
            }

            relationship.FromPersonId = newFromPersonId;
            relationship.ToPersonId = newToPersonId;
            movedRelationshipCount++;
        }

        Project.People.Remove(secondary);
        ReturnToFullTree();
        SelectedPerson = primary;
        RelationshipTargetPerson = Project.People.FirstOrDefault(person => person.Id != primary.Id);
        RebuildSelectedPersonRelationships();
        RebuildSelectedPersonNameVariants();
        RebuildSearchResults();
        OnPropertyChanged(nameof(People));
        OnPropertyChanged(nameof(Relationships));
        OnPropertyChanged(nameof(SelectedPersonRelationshipSummary));
        OnPropertyChanged(nameof(SelectedPersonNameVariantSummary));
        MarkProjectDirty();
        CommandManager.InvalidateRequerySuggested();
        RequestCanvasRefresh();

        var mergeNoteText = mergeNotes.Count == 0
            ? "- No special preservation rules were needed."
            : FormatReportList(mergeNotes);

        message =
            $"{secondary.EffectiveDisplayName} was merged into {primary.EffectiveDisplayName}.{Environment.NewLine}{Environment.NewLine}" +
            $"Relationships moved: {movedRelationshipCount}{Environment.NewLine}" +
            $"Duplicate or self-relationships removed: {removedRelationshipCount}{Environment.NewLine}{Environment.NewLine}" +
            $"Preservation notes:{Environment.NewLine}{mergeNoteText}";

        return true;
    }

    private string DescribeRelationshipMove(Relationship relationship, Person primaryPerson, Person secondaryPerson)
    {
        var newFromPersonId = relationship.FromPersonId == secondaryPerson.Id ? primaryPerson.Id : relationship.FromPersonId;
        var newToPersonId = relationship.ToPersonId == secondaryPerson.Id ? primaryPerson.Id : relationship.ToPersonId;
        var movedDescription = $"{relationship.Type}: {GetPersonName(newFromPersonId)} -> {GetPersonName(newToPersonId)} ({relationship.ParentKind})";

        if (newFromPersonId == newToPersonId)
        {
            return $"{DescribeRelationship(relationship)} -> would become a self-relationship and will be removed";
        }

        if (RelationshipDuplicateExistsAfterMerge(relationship, newFromPersonId, newToPersonId))
        {
            return $"{DescribeRelationship(relationship)} -> duplicates an existing relationship and will be removed";
        }

        return $"{DescribeRelationship(relationship)} -> {movedDescription}";
    }

    private static List<string> GetAliasesToAdd(Person primaryPerson, Person secondaryPerson)
    {
        var existingNames = GetSearchableNames(primaryPerson).ToList();
        var secondaryNames = new[] { secondaryPerson.Name, secondaryPerson.DisplayName }
            .Concat(secondaryPerson.AlsoKnownAs)
            .ToList();

        return GetDistinctValuesToAdd(existingNames, secondaryNames);
    }

    private static List<string> GetDistinctValuesToAdd(IEnumerable<string> existingValues, IEnumerable<string> newValues)
    {
        var existing = existingValues
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToList();

        var valuesToAdd = new List<string>();
        foreach (var value in newValues.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()))
        {
            if (existing.Any(existingValue => SameText(existingValue, value)) ||
                valuesToAdd.Any(existingValue => SameText(existingValue, value)))
            {
                continue;
            }

            valuesToAdd.Add(value);
        }

        return valuesToAdd;
    }

    private static void AddDistinct(List<string> values, string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            values.Any(existing => SameText(existing, value)))
        {
            return;
        }

        values.Add(value.Trim());
    }

    private static void MergeNotes(Person primaryPerson, Person secondaryPerson)
    {
        if (string.IsNullOrWhiteSpace(secondaryPerson.Notes))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(primaryPerson.Notes))
        {
            primaryPerson.Notes = secondaryPerson.Notes;
            return;
        }

        if (primaryPerson.Notes.Contains(secondaryPerson.Notes, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        primaryPerson.Notes =
            $"{primaryPerson.Notes.Trim()}{Environment.NewLine}{Environment.NewLine}" +
            $"Merged notes from {secondaryPerson.EffectiveDisplayName}:{Environment.NewLine}" +
            secondaryPerson.Notes.Trim();
    }

    private static string GetMergeNotesPreview(Person primaryPerson, Person secondaryPerson)
    {
        if (string.IsNullOrWhiteSpace(secondaryPerson.Notes))
        {
            return "Primary notes will be kept.";
        }

        if (string.IsNullOrWhiteSpace(primaryPerson.Notes))
        {
            return "Secondary notes will be copied to the primary person.";
        }

        return "Both people have notes. Secondary notes will be appended under a merge heading.";
    }

    private static string GetMergeGenerationPreview(Person primaryPerson, Person secondaryPerson)
    {
        if (!primaryPerson.GenerationOverride.HasValue && secondaryPerson.GenerationOverride.HasValue)
        {
            return $"Primary has no override, so secondary override {secondaryPerson.GenerationOverride.Value} will be copied.";
        }

        if (primaryPerson.GenerationOverride.HasValue && secondaryPerson.GenerationOverride.HasValue)
        {
            return $"Both have overrides. Primary override {primaryPerson.GenerationOverride.Value} will be kept.";
        }

        return "Primary generation placement will be kept.";
    }

    private static string GetMergeManualOffsetPreview(Person primaryPerson, Person secondaryPerson)
    {
        if (!HasManualOffset(primaryPerson) && HasManualOffset(secondaryPerson))
        {
            return $"Primary has no manual offsets, so secondary offsets X {secondaryPerson.ManualXOffset}, Y {secondaryPerson.ManualYOffset} will be copied.";
        }

        if (HasManualOffset(primaryPerson) && HasManualOffset(secondaryPerson))
        {
            return $"Both have manual offsets. Primary offsets X {primaryPerson.ManualXOffset}, Y {primaryPerson.ManualYOffset} will be kept.";
        }

        return "Primary manual offsets will be kept.";
    }

    private static bool HasManualOffset(Person person)
    {
        return Math.Abs(person.ManualXOffset) > 0.001 ||
               Math.Abs(person.ManualYOffset) > 0.001;
    }

    private bool RelationshipDuplicateExistsAfterMerge(Relationship candidate, string newFromPersonId, string newToPersonId)
    {
        return Project.Relationships.Any(existing =>
            !ReferenceEquals(existing, candidate) &&
            RelationshipsMatch(existing.Type, existing.FromPersonId, existing.ToPersonId, existing.ParentKind, candidate.Type, newFromPersonId, newToPersonId, candidate.ParentKind));
    }

    private static bool RelationshipsMatch(
        RelationshipType firstType,
        string firstFromPersonId,
        string firstToPersonId,
        ParentKind firstParentKind,
        RelationshipType secondType,
        string secondFromPersonId,
        string secondToPersonId,
        ParentKind secondParentKind)
    {
        if (firstType != secondType)
        {
            return false;
        }

        if (firstType == RelationshipType.Marriage)
        {
            return (firstFromPersonId == secondFromPersonId && firstToPersonId == secondToPersonId) ||
                   (firstFromPersonId == secondToPersonId && firstToPersonId == secondFromPersonId);
        }

        return firstFromPersonId == secondFromPersonId &&
               firstToPersonId == secondToPersonId &&
               firstParentKind == secondParentKind;
    }

    private static bool NameVariantsMatch(PersonNameVariant first, PersonNameVariant second)
    {
        return SameText(first.NameUsed, second.NameUsed) &&
               SameText(first.Book, second.Book) &&
               SameText(first.Reference, second.Reference) &&
               SameText(first.TranslationOrSource, second.TranslationOrSource) &&
               SameText(first.Notes, second.Notes);
    }

    private static bool SameText(string? first, string? second)
    {
        return string.Equals(first?.Trim(), second?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private async Task SaveProjectAsync()
    {
        if (!string.IsNullOrWhiteSpace(CurrentProjectFilePath))
        {
            await SaveProjectToFileAsync(CurrentProjectFilePath);
            return;
        }

        await SaveProjectAsAsync();
    }

    private async Task SaveProjectAsAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Bible Family Tree Project",
            Filter = "Bible Family Tree project (*.json)|*.json|All files (*.*)|*.*",
            FileName = string.IsNullOrWhiteSpace(CurrentProjectFilePath)
                ? "BibleFamilyTreeProject.json"
                : Path.GetFileName(CurrentProjectFilePath)
        };

        if (dialog.ShowDialog() == true)
        {
            await SaveProjectToFileAsync(dialog.FileName);
        }
    }

    private async Task SaveProjectToFileAsync(string filePath)
    {
        try
        {
            await _projectJsonService.SaveAsync(Project, filePath);
            CurrentProjectFilePath = filePath;
            HasUnsavedChanges = false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            MessageBox.Show(
                $"The project could not be saved.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Save failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task LoadProjectAsync()
    {
        if (!ConfirmContinueWithoutSaving(Application.Current.MainWindow))
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Load Bible Family Tree Project",
            Filter = "Bible Family Tree project (*.json)|*.json|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                ReturnToFullTree();
                Project = await _projectJsonService.LoadAsync(dialog.FileName);
                CurrentProjectFilePath = dialog.FileName;
                HasUnsavedChanges = false;
                SearchText = "";
                SelectedSearchResult = null;
                SelectedPerson = Project.People.FirstOrDefault();
                RelationshipTargetPerson = Project.People.FirstOrDefault(person => person.Id != SelectedPerson?.Id);
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException or IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(
                    $"This project file could not be loaded. Please choose a valid BibleFamilyTreeBuilder JSON file.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                    "Load failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    public bool ConfirmContinueWithoutSaving(Window? owner)
    {
        if (!HasUnsavedChanges)
        {
            return true;
        }

        var result = owner is null
            ? MessageBox.Show("You have unsaved changes. Continue without saving?", "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            : MessageBox.Show(owner, "You have unsaved changes. Continue without saving?", "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        return result == MessageBoxResult.Yes;
    }

    private async Task CreateManualBackupAsync()
    {
        if (!string.IsNullOrWhiteSpace(CurrentProjectFilePath))
        {
            try
            {
                var backupPath = GetBackupFilePath();
                await _projectJsonService.SaveAsync(Project, backupPath);
                MessageBox.Show(
                    Application.Current.MainWindow,
                    $"Backup created successfully.{Environment.NewLine}{Environment.NewLine}{backupPath}",
                    "Backup Created",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                MessageBox.Show(
                    Application.Current.MainWindow,
                    $"The backup could not be created.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                    "Backup Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Choose Backup Location",
            Filter = "Bible Family Tree project backup (*.json)|*.json|All files (*.*)|*.*",
            FileName = GetBackupFileName()
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await _projectJsonService.SaveAsync(Project, dialog.FileName);
            MessageBox.Show(
                Application.Current.MainWindow,
                $"Backup created successfully.{Environment.NewLine}{Environment.NewLine}{dialog.FileName}",
                "Backup Created",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            MessageBox.Show(
                Application.Current.MainWindow,
                $"The backup could not be created.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Backup Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OpenBackupsFolder()
    {
        if (string.IsNullOrWhiteSpace(CurrentProjectFilePath))
        {
            ShowFriendlyMessage("Save or load a project first so the app knows where its backups folder should be.", "No backups folder yet");
            return;
        }

        var backupFolder = GetBackupFolderPath();
        if (!Directory.Exists(backupFolder))
        {
            ShowFriendlyMessage("No backups folder exists for this project yet. Use Create Backup first.", "No backups folder yet");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = backupFolder,
                UseShellExecute = true
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(
                Application.Current.MainWindow,
                $"The backups folder could not be opened.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Open Backups Folder Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private bool TryCreateAutoBackupBeforeDestructiveAction(Window? owner, string actionDescription)
    {
        if (string.IsNullOrWhiteSpace(CurrentProjectFilePath))
        {
            var message = $"No automatic backup could be created before {actionDescription} because the project has not been saved yet.";
            if (owner is null)
            {
                MessageBox.Show(message, "No Backup Created", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(owner, message, "No Backup Created", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            return true;
        }

        try
        {
            _projectJsonService.Save(Project, GetBackupFilePath());
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            var message =
                $"An automatic backup could not be created before {actionDescription}.{Environment.NewLine}{Environment.NewLine}" +
                $"{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
                "Continue anyway?";

            var result = owner is null
                ? MessageBox.Show(message, "Backup Failed", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                : MessageBox.Show(owner, message, "Backup Failed", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            return result == MessageBoxResult.Yes;
        }
    }

    private string GetBackupFilePath()
    {
        var backupFolder = GetBackupFolderPath();
        Directory.CreateDirectory(backupFolder);
        return Path.Combine(backupFolder, GetBackupFileName());
    }

    private string GetBackupFolderPath()
    {
        var projectFolder = Path.GetDirectoryName(CurrentProjectFilePath);
        if (string.IsNullOrWhiteSpace(projectFolder))
        {
            projectFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        return Path.Combine(projectFolder, "backups");
    }

    private static string GetBackupFileName()
    {
        return $"bible-family-tree-backup-{DateTime.Now:yyyy-MM-dd-HHmm}.json";
    }

    private void AddParentChildRelationship()
    {
        if (!CanAddRelationship || SelectedPerson is null || RelationshipTargetPerson is null)
        {
            ShowFriendlyMessage("Choose two different people before adding a parent/child relationship.", "Relationship not added");
            return;
        }

        TryAddRelationship(RelationshipType.ParentChild, SelectedPerson.Id, RelationshipTargetPerson.Id, SelectedParentKind, SelectedParentKind.ToString());
    }

    private void AddMarriageRelationship()
    {
        if (!CanAddRelationship || SelectedPerson is null || RelationshipTargetPerson is null)
        {
            ShowFriendlyMessage("Choose two different people before adding a marriage relationship.", "Relationship not added");
            return;
        }

        TryAddRelationship(RelationshipType.Marriage, SelectedPerson.Id, RelationshipTargetPerson.Id, ParentKind.Unknown, "Marriage");
    }

    private Person? CreatePersonWithDuplicateWarning(string name, CardType cardType)
    {
        var duplicateNames = FindPeopleWithMatchingName(name).ToList();
        if (duplicateNames.Count > 0)
        {
            var result = MessageBox.Show(
                Application.Current.MainWindow,
                $"A person named \"{name}\" may already exist.{Environment.NewLine}{Environment.NewLine}Biblical names often repeat, so you can still continue.",
                "Possible duplicate name",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return null;
            }
        }

        return new Person
        {
            Name = name,
            DisplayName = name,
            CardType = cardType
        };
    }

    private IEnumerable<Person> FindPeopleWithMatchingName(string name)
    {
        return Project.People.Where(person => GetSearchableNames(person)
            .Any(existingName => string.Equals(existingName, name, StringComparison.OrdinalIgnoreCase)));
    }

    private void AddNameVariant()
    {
        if (SelectedPerson is null)
        {
            ShowFriendlyMessage("Select a person before adding a name variant.", "No person selected");
            return;
        }

        if (string.IsNullOrWhiteSpace(NameVariantNameUsedText))
        {
            ShowFriendlyMessage("Enter the name or spelling used before adding the variant.", "Name variant not added");
            return;
        }

        var variant = new PersonNameVariant
        {
            NameUsed = NameVariantNameUsedText.Trim(),
            Book = NameVariantBookText.Trim(),
            Reference = NameVariantReferenceText.Trim(),
            Notes = NameVariantNotesText.Trim(),
            TranslationOrSource = NameVariantTranslationOrSourceText.Trim()
        };

        SelectedPerson.NameVariants.Add(variant);
        RebuildSelectedPersonNameVariants(variant);
        NotifyNameVariantsChanged();
    }

    private void SaveSelectedNameVariant()
    {
        if (SelectedNameVariantItem is null)
        {
            ShowFriendlyMessage("Select a name variant before saving changes.", "No name variant selected");
            return;
        }

        var variant = SelectedNameVariantItem.Variant;
        variant.Id = string.IsNullOrWhiteSpace(variant.Id) ? Guid.NewGuid().ToString("N") : variant.Id;
        variant.NameUsed = NameVariantNameUsedText.Trim();
        variant.Book = NameVariantBookText.Trim();
        variant.Reference = NameVariantReferenceText.Trim();
        variant.Notes = NameVariantNotesText.Trim();
        variant.TranslationOrSource = NameVariantTranslationOrSourceText.Trim();

        RebuildSelectedPersonNameVariants(variant);
        NotifyNameVariantsChanged();
    }

    private void ConfirmAndDeleteSelectedNameVariant()
    {
        if (SelectedPerson is null || SelectedNameVariantItem is null)
        {
            ShowFriendlyMessage("Select a name variant before deleting it.", "No name variant selected");
            return;
        }

        var result = MessageBox.Show(
            Application.Current.MainWindow,
            "Delete this name variant? This only removes the book-specific name entry. It will not delete the person.",
            "Delete name variant",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        SelectedPerson.NameVariants.Remove(SelectedNameVariantItem.Variant);
        RebuildSelectedPersonNameVariants();
        ClearNameVariantEditor();
        NotifyNameVariantsChanged();
    }

    private void LoadRelationshipEditor(Relationship? relationship)
    {
        RelationshipDisplayLabelText = relationship?.DisplayLabel ?? "";
        SelectedRelationshipParentKind = relationship?.ParentKind ?? ParentKind.Unknown;
        SelectedRelationshipEvidenceLevel = relationship?.EvidenceLevel ?? EvidenceLevel.Unknown;
        RelationshipBibleReferencesText = relationship is null ? "" : string.Join(Environment.NewLine, relationship.BibleReferences);
        RelationshipNotesText = relationship?.Notes ?? "";
    }

    private void ClearRelationshipEditor()
    {
        LoadRelationshipEditor(null);
    }

    private void SaveSelectedRelationshipChanges()
    {
        if (SelectedRelationshipItem is null)
        {
            ShowFriendlyMessage("Select a relationship before saving changes.", "No relationship selected");
            return;
        }

        var relationship = SelectedRelationshipItem.Relationship;
        relationship.DisplayLabel = RelationshipDisplayLabelText.Trim();
        relationship.EvidenceLevel = SelectedRelationshipEvidenceLevel;
        relationship.BibleReferences = SplitLines(RelationshipBibleReferencesText);
        relationship.Notes = RelationshipNotesText.Trim();

        if (relationship.Type == RelationshipType.ParentChild)
        {
            relationship.ParentKind = SelectedRelationshipParentKind;
        }

        RebuildSelectedPersonRelationships(relationship);
        OnPropertyChanged(nameof(SelectedPersonRelationshipSummary));
        RebuildSearchResults();
        MarkProjectDirty();
        RefreshDisplayProject();
        RequestCanvasRefresh();
    }

    private bool TryAddRelationship(
        RelationshipType relationshipType,
        string fromPersonId,
        string toPersonId,
        ParentKind parentKind,
        string label)
    {
        if (string.IsNullOrWhiteSpace(fromPersonId) || string.IsNullOrWhiteSpace(toPersonId))
        {
            ShowFriendlyMessage("Both people must exist before a relationship can be added.", "Relationship not added");
            return false;
        }

        if (fromPersonId == toPersonId)
        {
            var message = relationshipType == RelationshipType.Marriage
                ? "A person cannot be married to themselves."
                : "A person cannot be their own parent or child.";
            ShowFriendlyMessage(message, "Relationship not added");
            return false;
        }

        if (!Project.People.Any(person => person.Id == fromPersonId) || !Project.People.Any(person => person.Id == toPersonId))
        {
            ShowFriendlyMessage("The relationship points to a missing person. Please choose people that exist in the project.", "Relationship not added");
            return false;
        }

        if (RelationshipAlreadyExists(relationshipType, fromPersonId, toPersonId, parentKind))
        {
            ShowFriendlyMessage("That relationship already exists, so it was not added again.", "Duplicate relationship");
            return false;
        }

        Project.Relationships.Add(new Relationship
        {
            Type = relationshipType,
            FromPersonId = fromPersonId,
            ToPersonId = toPersonId,
            ParentKind = parentKind,
            Label = label
        });

        return true;
    }

    private bool RelationshipAlreadyExists(RelationshipType relationshipType, string fromPersonId, string toPersonId, ParentKind parentKind)
    {
        return Project.Relationships.Any(relationship =>
        {
            if (relationship.Type != relationshipType)
            {
                return false;
            }

            if (relationshipType == RelationshipType.Marriage)
            {
                return (relationship.FromPersonId == fromPersonId && relationship.ToPersonId == toPersonId) ||
                       (relationship.FromPersonId == toPersonId && relationship.ToPersonId == fromPersonId);
            }

            return relationship.FromPersonId == fromPersonId &&
                   relationship.ToPersonId == toPersonId &&
                   relationship.ParentKind == parentKind;
        });
    }

    public void ConfirmAndDeleteSelectedRelationship(Window? owner)
    {
        if (SelectedRelationshipItem is null)
        {
            ShowFriendlyMessage("Select a relationship first.", "No relationship selected");
            return;
        }

        var result = owner is null
            ? MessageBox.Show($"Delete this relationship?{Environment.NewLine}{Environment.NewLine}{SelectedRelationshipItem.DisplayText}", "Confirm relationship delete", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            : MessageBox.Show(owner, $"Delete this relationship?{Environment.NewLine}{Environment.NewLine}{SelectedRelationshipItem.DisplayText}", "Confirm relationship delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (!TryCreateAutoBackupBeforeDestructiveAction(owner, "deleting a relationship"))
        {
            return;
        }

        Project.Relationships.Remove(SelectedRelationshipItem.Relationship);
        SelectedRelationshipItem = null;
    }

    private void NudgeSelectedPerson(double xOffset, double yOffset)
    {
        if (SelectedPerson is null)
        {
            return;
        }

        SelectedPerson.ManualXOffset += xOffset;
        SelectedPerson.ManualYOffset += yOffset;
        MarkProjectDirty();
        RequestCanvasRefresh();
    }

    private void ClearManualPosition()
    {
        if (SelectedPerson is null)
        {
            return;
        }

        SelectedPerson.ManualXOffset = 0;
        SelectedPerson.ManualYOffset = 0;
        MarkProjectDirty();
        RequestCanvasRefresh();
    }

    private int? GetSelectedEffectiveGeneration()
    {
        if (SelectedPerson is null)
        {
            return null;
        }

        var layout = _treeLayoutService.Layout(Project);
        return layout.PersonGenerations.TryGetValue(SelectedPerson.Id, out var generation)
            ? generation
            : null;
    }

    private GenerationLabel? GetSelectedGenerationLabel()
    {
        var generation = GetSelectedEffectiveGeneration();
        return generation.HasValue
            ? Project.GenerationLabels.FirstOrDefault(label => label.GenerationNumber == generation.Value)
            : null;
    }

    private void UpdateSelectedGenerationLabel(string? title, string? notes)
    {
        var generation = GetSelectedEffectiveGeneration();
        if (!generation.HasValue)
        {
            return;
        }

        var label = Project.GenerationLabels.FirstOrDefault(existingLabel => existingLabel.GenerationNumber == generation.Value);
        if (label is null)
        {
            label = new GenerationLabel { GenerationNumber = generation.Value };
            Project.GenerationLabels.Add(label);
        }

        if (title is not null)
        {
            label.Title = title;
        }

        if (notes is not null)
        {
            label.Notes = notes;
        }

        if (string.IsNullOrWhiteSpace(label.Title) && string.IsNullOrWhiteSpace(label.Notes))
        {
            Project.GenerationLabels.Remove(label);
        }

        MarkProjectDirty();
        NotifyGenerationLabelProperties();
        RefreshDisplayProject();
        RequestCanvasRefresh();
    }

    private void ClearSelectedGenerationLabel()
    {
        var label = GetSelectedGenerationLabel();
        if (label is null)
        {
            return;
        }

        Project.GenerationLabels.Remove(label);
        MarkProjectDirty();
        NotifyGenerationLabelProperties();
        RefreshDisplayProject();
        RequestCanvasRefresh();
    }

    private void NotifyGenerationLabelProperties()
    {
        OnPropertyChanged(nameof(SelectedEffectiveGenerationText));
        OnPropertyChanged(nameof(SelectedGenerationOverrideStatusText));
        OnPropertyChanged(nameof(SelectedGenerationLabelTitle));
        OnPropertyChanged(nameof(SelectedGenerationLabelNotes));
    }

    private void AttachProjectEvents()
    {
        Project.People.CollectionChanged += PeopleChanged;
        Project.Relationships.CollectionChanged += RelationshipsChanged;

        foreach (var person in Project.People)
        {
            person.PropertyChanged += PersonChanged;
        }
    }

    private void DetachProjectEvents()
    {
        Project.People.CollectionChanged -= PeopleChanged;
        Project.Relationships.CollectionChanged -= RelationshipsChanged;

        foreach (var person in Project.People)
        {
            person.PropertyChanged -= PersonChanged;
        }
    }

    private void PeopleChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (Person person in e.OldItems)
            {
                person.PropertyChanged -= PersonChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (Person person in e.NewItems)
            {
                person.PropertyChanged += PersonChanged;
            }
        }

        OnPropertyChanged(nameof(People));
        MarkProjectDirty();
        NotifyGenerationLabelProperties();
        RebuildSearchResults();
        RefreshDisplayProject();
        CommandManager.InvalidateRequerySuggested();
        RequestCanvasRefresh();
    }

    private void RelationshipsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Relationships));
        OnPropertyChanged(nameof(SelectedPersonRelationshipSummary));
        MarkProjectDirty();
        NotifyGenerationLabelProperties();
        RebuildSelectedPersonRelationships();
        RefreshDisplayProject();
        CommandManager.InvalidateRequerySuggested();
        RequestCanvasRefresh();
    }

    private void PersonChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(SelectedAlsoKnownAsText));
        OnPropertyChanged(nameof(SelectedBibleReferencesText));
        OnPropertyChanged(nameof(SelectedGenerationOverrideText));
        OnPropertyChanged(nameof(SelectedPersonRelationshipSummary));
        OnPropertyChanged(nameof(SelectedPersonNameVariantSummary));
        MarkProjectDirty();
        NotifyGenerationLabelProperties();
        RebuildSelectedPersonRelationships();
        RebuildSelectedPersonNameVariants();
        RebuildSearchResults();
        RefreshDisplayProject();
        RequestCanvasRefresh();
    }

    private void RebuildSelectedPersonRelationships(Relationship? relationshipToSelect = null)
    {
        SelectedPersonRelationships.Clear();
        SelectedRelationshipItem = null;

        if (SelectedPerson is null)
        {
            return;
        }

        foreach (var relationship in Project.Relationships.Where(IsConnectedToSelectedPerson))
        {
            var item = CreateRelationshipListItem(relationship);
            SelectedPersonRelationships.Add(item);

            if (relationshipToSelect is not null && ReferenceEquals(relationship, relationshipToSelect))
            {
                SelectedRelationshipItem = item;
            }
        }

        OnPropertyChanged(nameof(SelectedPersonRelationships));
    }

    private void RebuildSelectedPersonNameVariants(PersonNameVariant? variantToSelect = null)
    {
        SelectedPersonNameVariants.Clear();
        SelectedNameVariantItem = null;

        if (SelectedPerson is null)
        {
            OnPropertyChanged(nameof(SelectedPersonNameVariants));
            return;
        }

        foreach (var variant in SelectedPerson.NameVariants.OrderBy(variant => variant.NameUsed, StringComparer.OrdinalIgnoreCase))
        {
            var item = new NameVariantListItem(variant, FormatNameVariantListText(variant));
            SelectedPersonNameVariants.Add(item);

            if (variantToSelect is not null && ReferenceEquals(variant, variantToSelect))
            {
                SelectedNameVariantItem = item;
            }
        }

        OnPropertyChanged(nameof(SelectedPersonNameVariants));
    }

    private void LoadNameVariantEditor(PersonNameVariant? variant)
    {
        NameVariantNameUsedText = variant?.NameUsed ?? "";
        NameVariantBookText = variant?.Book ?? "";
        NameVariantReferenceText = variant?.Reference ?? "";
        NameVariantNotesText = variant?.Notes ?? "";
        NameVariantTranslationOrSourceText = variant?.TranslationOrSource ?? "";
    }

    private void ClearNameVariantEditor()
    {
        LoadNameVariantEditor(null);
    }

    private void NotifyNameVariantsChanged()
    {
        MarkProjectDirty();
        OnPropertyChanged(nameof(SelectedPersonNameVariantSummary));
        RebuildSearchResults();
        CommandManager.InvalidateRequerySuggested();
    }

    private bool IsConnectedToSelectedPerson(Relationship relationship)
    {
        return SelectedPerson is not null &&
               (relationship.FromPersonId == SelectedPerson.Id || relationship.ToPersonId == SelectedPerson.Id);
    }

    private RelationshipListItem CreateRelationshipListItem(Relationship relationship)
    {
        if (SelectedPerson is null)
        {
            return new RelationshipListItem(relationship, "Other", "No person selected");
        }

        if (relationship.Type == RelationshipType.Marriage)
        {
            var otherPersonId = relationship.FromPersonId == SelectedPerson.Id ? relationship.ToPersonId : relationship.FromPersonId;
            return new RelationshipListItem(relationship, "Spouses", $"Spouses - {FormatRelationshipListText(relationship, relationship.FromPersonId, relationship.ToPersonId, isMarriage: true)}");
        }

        if (relationship.Type == RelationshipType.ParentChild && relationship.ToPersonId == SelectedPerson.Id)
        {
            return new RelationshipListItem(relationship, "Parents", $"Parents - {FormatRelationshipListText(relationship, relationship.FromPersonId, relationship.ToPersonId, isMarriage: false)}");
        }

        if (relationship.Type == RelationshipType.ParentChild && relationship.FromPersonId == SelectedPerson.Id)
        {
            return new RelationshipListItem(relationship, "Children", $"Children - {FormatRelationshipListText(relationship, relationship.FromPersonId, relationship.ToPersonId, isMarriage: false)}");
        }

        var otherId = relationship.FromPersonId == SelectedPerson.Id ? relationship.ToPersonId : relationship.FromPersonId;
        return new RelationshipListItem(relationship, "Other", $"Other - {FormatRelationshipListText(relationship, relationship.FromPersonId, relationship.ToPersonId, relationship.Type == RelationshipType.Marriage)}");
    }

    private string FormatRelationshipListText(Relationship relationship, string fromPersonId, string toPersonId, bool isMarriage)
    {
        var connector = isMarriage ? "<->" : "->";
        var typeDetail = isMarriage ? relationship.Type.ToString() : $"{relationship.Type} | {relationship.ParentKind}";
        var displayLabel = string.IsNullOrWhiteSpace(relationship.DisplayLabel) ? "" : $" | {relationship.DisplayLabel}";
        return $"{typeDetail}: {GetPersonName(fromPersonId)} {connector} {GetPersonName(toPersonId)} | {relationship.EvidenceLevel}{displayLabel}";
    }

    private string GetPersonName(string personId)
    {
        return Project.People.FirstOrDefault(person => person.Id == personId)?.EffectiveDisplayName ?? "Missing person";
    }

    private void ShowTreeCheckReport()
    {
        var peopleWithNoRelationships = Project.People
            .Where(person => Project.Relationships.All(relationship => relationship.FromPersonId != person.Id && relationship.ToPersonId != person.Id))
            .Select(person => person.EffectiveDisplayName)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var peopleIds = Project.People.Select(person => person.Id).ToHashSet();
        var relationshipsPointingToMissingPeople = Project.Relationships
            .Where(relationship => string.IsNullOrWhiteSpace(relationship.FromPersonId) ||
                                   string.IsNullOrWhiteSpace(relationship.ToPersonId) ||
                                   !peopleIds.Contains(relationship.FromPersonId) ||
                                   !peopleIds.Contains(relationship.ToPersonId))
            .ToList();

        var duplicatePersonIds = Project.People
            .GroupBy(person => person.Id)
            .Where(group => group.Count() > 1)
            .Select(group => $"Person ID {group.Key} ({group.Count()})")
            .ToList();

        var duplicateRelationshipIds = Project.Relationships
            .GroupBy(relationship => relationship.Id)
            .Where(group => group.Count() > 1)
            .Select(group => $"Relationship ID {group.Key} ({group.Count()})")
            .ToList();

        var duplicateIds = duplicatePersonIds.Concat(duplicateRelationshipIds).ToList();

        var possibleDuplicateNames = Project.People
            .SelectMany(person => GetSearchableNames(person)
                .Select(name => new { Name = name, PersonId = person.Id }))
            .GroupBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Select(entry => entry.PersonId).Distinct().Count() > 1)
            .Select(group =>
            {
                var people = group
                    .Select(entry => entry.PersonId)
                    .Distinct()
                    .Select(GetPersonName)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
                return $"{group.Key}: {string.Join(", ", people)}";
            })
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var peopleWithNameVariants = Project.People.Count(person => person.NameVariants.Count > 0);
        var totalNameVariants = Project.People.Sum(person => person.NameVariants.Count);

        var nameVariantsMissingName = Project.People
            .SelectMany(person => person.NameVariants
                .Where(variant => string.IsNullOrWhiteSpace(variant.NameUsed))
                .Select(variant => $"{person.EffectiveDisplayName}: {FormatNameVariantListText(variant)}"))
            .ToList();

        var nameVariantsMissingBook = Project.People
            .SelectMany(person => person.NameVariants
                .Where(variant => string.IsNullOrWhiteSpace(variant.Book))
                .Select(variant => $"{person.EffectiveDisplayName}: {FormatNameVariantListText(variant)}"))
            .ToList();

        var relationshipsWithBibleReferences = Project.Relationships
            .Count(relationship => relationship.BibleReferences.Count > 0);

        var relationshipsMissingEvidenceLevel = Project.Relationships
            .Where(relationship => relationship.EvidenceLevel == EvidenceLevel.Unknown)
            .Select(DescribeRelationship)
            .ToList();

        var uncertainRelationships = Project.Relationships
            .Where(relationship => relationship.EvidenceLevel == EvidenceLevel.Uncertain)
            .Select(DescribeRelationship)
            .ToList();

        var relationshipsWithNotes = Project.Relationships
            .Where(relationship => !string.IsNullOrWhiteSpace(relationship.Notes))
            .Select(DescribeRelationship)
            .ToList();

        var relationshipsWithDisplayLabel = Project.Relationships
            .Where(relationship => !string.IsNullOrWhiteSpace(relationship.DisplayLabel))
            .Select(relationship => $"{DescribeRelationship(relationship)} - {relationship.DisplayLabel}")
            .ToList();

        var parentChildRelationshipsWithUnknownParentKind = Project.Relationships
            .Where(relationship => relationship.Type == RelationshipType.ParentChild && relationship.ParentKind == ParentKind.Unknown)
            .Select(DescribeRelationship)
            .ToList();

        var generationOverrides = Project.People
            .Where(person => person.GenerationOverride.HasValue)
            .Select(person => $"{person.EffectiveDisplayName}: {person.GenerationOverride}")
            .ToList();

        var manualOffsets = Project.People
            .Where(person => Math.Abs(person.ManualXOffset) > 0.001 || Math.Abs(person.ManualYOffset) > 0.001)
            .Select(person => $"{person.EffectiveDisplayName}: X {person.ManualXOffset}, Y {person.ManualYOffset}")
            .ToList();

        var generationLabels = Project.GenerationLabels
            .OrderBy(label => label.GenerationNumber)
            .Select(label =>
            {
                var title = string.IsNullOrWhiteSpace(label.Title) ? "(no title)" : label.Title;
                var notes = string.IsNullOrWhiteSpace(label.Notes) ? "" : $" - {label.Notes}";
                return $"Generation {label.GenerationNumber}: {title}{notes}";
            })
            .ToList();

        var report =
            $"People: {Project.People.Count}{Environment.NewLine}" +
            $"Relationships: {Project.Relationships.Count}{Environment.NewLine}" +
            $"Custom generation labels: {Project.GenerationLabels.Count}{Environment.NewLine}{Environment.NewLine}" +
            $"People with name variants: {peopleWithNameVariants}{Environment.NewLine}" +
            $"Total name variant entries: {totalNameVariants}{Environment.NewLine}{Environment.NewLine}" +
            $"Relationships with Bible references: {relationshipsWithBibleReferences}{Environment.NewLine}{Environment.NewLine}" +
            $"People with no relationships:{Environment.NewLine}{FormatReportList(peopleWithNoRelationships)}{Environment.NewLine}{Environment.NewLine}" +
            $"Relationships pointing to missing people:{Environment.NewLine}{FormatReportList(relationshipsPointingToMissingPeople.Select(DescribeRelationship).ToList())}{Environment.NewLine}{Environment.NewLine}" +
            $"Duplicate IDs:{Environment.NewLine}{FormatReportList(duplicateIds)}{Environment.NewLine}{Environment.NewLine}" +
            $"Possible duplicate people by name, alias, or variant (use Merge People if they are the same person):{Environment.NewLine}{FormatReportList(possibleDuplicateNames)}{Environment.NewLine}{Environment.NewLine}" +
            $"Name variants missing NameUsed:{Environment.NewLine}{FormatReportList(nameVariantsMissingName)}{Environment.NewLine}{Environment.NewLine}" +
            $"Name variants missing Book:{Environment.NewLine}{FormatReportList(nameVariantsMissingBook)}{Environment.NewLine}{Environment.NewLine}" +
            $"Relationships missing evidence level:{Environment.NewLine}{FormatReportList(relationshipsMissingEvidenceLevel)}{Environment.NewLine}{Environment.NewLine}" +
            $"Relationships marked Uncertain:{Environment.NewLine}{FormatReportList(uncertainRelationships)}{Environment.NewLine}{Environment.NewLine}" +
            $"Relationships with notes:{Environment.NewLine}{FormatReportList(relationshipsWithNotes)}{Environment.NewLine}{Environment.NewLine}" +
            $"Relationships with DisplayLabel:{Environment.NewLine}{FormatReportList(relationshipsWithDisplayLabel)}{Environment.NewLine}{Environment.NewLine}" +
            $"ParentChild relationships with ParentKind Unknown:{Environment.NewLine}{FormatReportList(parentChildRelationshipsWithUnknownParentKind)}{Environment.NewLine}{Environment.NewLine}" +
            $"Generation labels:{Environment.NewLine}{FormatReportList(generationLabels)}{Environment.NewLine}{Environment.NewLine}" +
            $"People with GenerationOverride set:{Environment.NewLine}{FormatReportList(generationOverrides)}{Environment.NewLine}{Environment.NewLine}" +
            $"People with manual offsets:{Environment.NewLine}{FormatReportList(manualOffsets)}";

        MessageBox.Show(Application.Current.MainWindow, report, "Check Tree", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private string DescribeRelationship(Relationship relationship)
    {
        return $"{relationship.Type}: {GetPersonName(relationship.FromPersonId)} -> {GetPersonName(relationship.ToPersonId)} ({relationship.ParentKind})";
    }

    private static string FormatReportList(List<string> lines)
    {
        return lines.Count == 0 ? "- None" : string.Join(Environment.NewLine, lines.Select(line => $"- {line}"));
    }

    private static void ShowFriendlyMessage(string message, string title)
    {
        MessageBox.Show(Application.Current.MainWindow, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RebuildSearchResults()
    {
        SearchResults.Clear();

        var search = SearchText.Trim();
        var matches = string.IsNullOrWhiteSpace(search)
            ? Project.People
            : GetPeopleMatchingSearch(search);

        foreach (var person in matches.OrderBy(person => person.EffectiveDisplayName, StringComparer.OrdinalIgnoreCase))
        {
            SearchResults.Add(person);
        }

        OnPropertyChanged(nameof(SearchResults));
        SyncSelectedSearchResult();
    }

    private IEnumerable<Person> GetPeopleMatchingSearch(string search)
    {
        var matchingPersonIds = Project.People
            .Where(person => PersonMatchesSearch(person, search))
            .Select(person => person.Id)
            .ToHashSet();

        foreach (var relationship in Project.Relationships.Where(relationship => RelationshipMatchesSearch(relationship, search)))
        {
            if (SelectedPerson is not null &&
                (relationship.FromPersonId == SelectedPerson.Id || relationship.ToPersonId == SelectedPerson.Id))
            {
                matchingPersonIds.Add(SelectedPerson.Id);
            }
            else
            {
                matchingPersonIds.Add(relationship.FromPersonId);
            }
        }

        return Project.People.Where(person => matchingPersonIds.Contains(person.Id));
    }

    private static bool PersonMatchesSearch(Person person, string search)
    {
        return Contains(person.Name, search) ||
               Contains(person.DisplayName, search) ||
               person.AlsoKnownAs.Any(alias => Contains(alias, search)) ||
               person.BibleReferences.Any(reference => Contains(reference, search)) ||
               person.NameVariants.Any(variant =>
                   Contains(variant.NameUsed, search) ||
                   Contains(variant.Book, search) ||
                   Contains(variant.Reference, search) ||
                   Contains(variant.Notes, search) ||
                   Contains(variant.TranslationOrSource, search));
    }

    private static bool RelationshipMatchesSearch(Relationship relationship, string search)
    {
        return Contains(relationship.DisplayLabel, search) ||
               relationship.BibleReferences.Any(reference => Contains(reference, search)) ||
               Contains(relationship.Notes, search) ||
               Contains(relationship.EvidenceLevel.ToString(), search);
    }

    private static bool Contains(string? value, string search)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> GetSearchableNames(Person person)
    {
        if (!string.IsNullOrWhiteSpace(person.Name))
        {
            yield return person.Name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(person.DisplayName))
        {
            yield return person.DisplayName.Trim();
        }

        foreach (var alias in person.AlsoKnownAs.Where(alias => !string.IsNullOrWhiteSpace(alias)))
        {
            yield return alias.Trim();
        }

        foreach (var variant in person.NameVariants.Where(variant => !string.IsNullOrWhiteSpace(variant.NameUsed)))
        {
            yield return variant.NameUsed.Trim();
        }
    }

    private static string FormatNameVariantSummaryLine(PersonNameVariant variant)
    {
        var name = string.IsNullOrWhiteSpace(variant.NameUsed) ? "(missing name)" : variant.NameUsed;
        var sourceParts = new[] { variant.Book, variant.Reference }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();
        var source = sourceParts.Count == 0 ? "" : $" - {string.Join(" ", sourceParts)}";
        return $"- {name}{source}";
    }

    private static string FormatNameVariantListText(PersonNameVariant variant)
    {
        var name = string.IsNullOrWhiteSpace(variant.NameUsed) ? "(missing name)" : variant.NameUsed;
        var book = string.IsNullOrWhiteSpace(variant.Book) ? "(missing book)" : variant.Book;
        var reference = string.IsNullOrWhiteSpace(variant.Reference) ? "" : $" {variant.Reference}";
        return $"{name} - {book}{reference}";
    }

    private string FormatRelatedPerson(string personId, string detail)
    {
        var person = Project.People.FirstOrDefault(candidate => candidate.Id == personId);
        var name = person?.EffectiveDisplayName ?? "Missing person";
        return $"- {name} ({detail})";
    }

    private static string FormatSummaryList(List<string> lines)
    {
        return lines.Count == 0 ? "- None" : string.Join(Environment.NewLine, lines);
    }

    private void SyncSelectedSearchResult()
    {
        var matchingSearchResult = SelectedPerson is null
            ? null
            : SearchResults.FirstOrDefault(person => person.Id == SelectedPerson.Id);

        if (_selectedSearchResult == matchingSearchResult)
        {
            return;
        }

        _selectedSearchResult = matchingSearchResult;
        OnPropertyChanged(nameof(SelectedSearchResult));
    }

    private static List<string> SplitLines(string value)
    {
        return value
            .Split([Environment.NewLine, "\n", ","], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    private void RequestCanvasRefresh()
    {
        CanvasRefreshRequested?.Invoke();
    }

    private void MarkProjectDirty()
    {
        HasUnsavedChanges = true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            return canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object? parameter)
        {
            execute(parameter);
        }
    }
}

public class RelationshipListItem(Relationship relationship, string groupName, string displayText)
{
    public Relationship Relationship { get; } = relationship;
    public string GroupName { get; } = groupName;
    public string DisplayText { get; } = displayText;
}

public class NameVariantListItem(PersonNameVariant variant, string displayText)
{
    public PersonNameVariant Variant { get; } = variant;
    public string DisplayText { get; } = displayText;
}

public enum LineageViewMode
{
    FullTree,
    Ancestors,
    Descendants,
    AncestorsAndDescendants
}
