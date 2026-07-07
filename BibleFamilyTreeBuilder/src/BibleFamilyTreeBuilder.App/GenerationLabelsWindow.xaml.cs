using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using BibleFamilyTreeBuilder.App.Models;
using BibleFamilyTreeBuilder.App.Services;

namespace BibleFamilyTreeBuilder.App;

public partial class GenerationLabelsWindow : Window
{
    private readonly TreeProject _project;
    private readonly Action _generationLabelsChanged;
    private readonly TreeLayoutService _layoutService = new();
    private readonly ObservableCollection<GenerationLabelRow> _rows = [];
    private GenerationLabel? _selectedLabel;

    public GenerationLabelsWindow(TreeProject project, Action generationLabelsChanged)
    {
        InitializeComponent();

        _project = project;
        _generationLabelsChanged = generationLabelsChanged;
        LabelsGrid.ItemsSource = _rows;
        RefreshRows();
    }

    private void RefreshRows(GenerationLabel? labelToSelect = null)
    {
        var peopleByGeneration = _layoutService.Layout(_project).PersonGenerations
            .GroupBy(entry => entry.Value)
            .ToDictionary(group => group.Key, group => group.Count());

        _rows.Clear();
        foreach (var label in _project.GenerationLabels.OrderBy(label => label.GenerationNumber))
        {
            peopleByGeneration.TryGetValue(label.GenerationNumber, out var peopleCount);
            _rows.Add(new GenerationLabelRow(label, peopleCount));
        }

        if (labelToSelect is not null)
        {
            LabelsGrid.SelectedItem = _rows.FirstOrDefault(row => ReferenceEquals(row.Label, labelToSelect));
        }
    }

    private void LabelsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LabelsGrid.SelectedItem is not GenerationLabelRow row)
        {
            return;
        }

        _selectedLabel = row.Label;
        GenerationNumberBox.Text = row.GenerationNumber.ToString();
        TitleBox.Text = row.Title;
        NotesBox.Text = row.Notes;
    }

    private void AddLabel_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadGenerationNumber(out var generationNumber))
        {
            return;
        }

        if (FindDuplicate(generationNumber, except: null) is not null)
        {
            ShowMessage($"Generation {generationNumber} already has a label. Select that row and edit the existing label instead.", "Duplicate Generation");
            return;
        }

        var label = new GenerationLabel
        {
            GenerationNumber = generationNumber,
            Title = TitleBox.Text.Trim(),
            Notes = NotesBox.Text.Trim()
        };

        _project.GenerationLabels.Add(label);
        NotifyChangedAndRefresh(label);
    }

    private void SaveChanges_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedLabel is null)
        {
            ShowMessage("Select a generation label before saving changes.", "No Label Selected");
            return;
        }

        if (!TryReadGenerationNumber(out var generationNumber))
        {
            return;
        }

        if (FindDuplicate(generationNumber, _selectedLabel) is not null)
        {
            ShowMessage($"Generation {generationNumber} already has a label. Edit that existing label or choose a different generation number.", "Duplicate Generation");
            return;
        }

        _selectedLabel.GenerationNumber = generationNumber;
        _selectedLabel.Title = TitleBox.Text.Trim();
        _selectedLabel.Notes = NotesBox.Text.Trim();

        NotifyChangedAndRefresh(_selectedLabel);
    }

    private void DeleteSelectedLabel_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedLabel is null)
        {
            ShowMessage("Select a generation label before deleting it.", "No Label Selected");
            return;
        }

        var result = MessageBox.Show(
            this,
            "Delete this generation label? This only removes the row title and notes. It will not delete people or remove any GenerationOverride values.",
            "Delete Generation Label",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        _project.GenerationLabels.Remove(_selectedLabel);
        _selectedLabel = null;
        ClearFormFields();
        NotifyChangedAndRefresh();
    }

    private void ClearForm_Click(object sender, RoutedEventArgs e)
    {
        LabelsGrid.SelectedItem = null;
        _selectedLabel = null;
        ClearFormFields();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void NotifyChangedAndRefresh(GenerationLabel? labelToSelect = null)
    {
        _generationLabelsChanged();
        RefreshRows(labelToSelect);
    }

    private bool TryReadGenerationNumber(out int generationNumber)
    {
        if (int.TryParse(GenerationNumberBox.Text.Trim(), out generationNumber))
        {
            return true;
        }

        ShowMessage("Enter a whole number for the generation number.", "Invalid Generation Number");
        return false;
    }

    private GenerationLabel? FindDuplicate(int generationNumber, GenerationLabel? except)
    {
        return _project.GenerationLabels.FirstOrDefault(label =>
            label.GenerationNumber == generationNumber &&
            !ReferenceEquals(label, except));
    }

    private void ClearFormFields()
    {
        GenerationNumberBox.Text = "";
        TitleBox.Text = "";
        NotesBox.Text = "";
    }

    private void ShowMessage(string message, string title)
    {
        MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

public sealed class GenerationLabelRow
{
    public GenerationLabelRow(GenerationLabel label, int peopleCount)
    {
        Label = label;
        PeopleCount = peopleCount;
    }

    public GenerationLabel Label { get; }
    public int GenerationNumber => Label.GenerationNumber;
    public string Title => Label.Title;
    public string Notes => Label.Notes;
    public int PeopleCount { get; }
}
