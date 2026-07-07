using System.Windows;
using System.Windows.Controls;
using BibleFamilyTreeBuilder.App.Models;
using BibleFamilyTreeBuilder.App.ViewModels;

namespace BibleFamilyTreeBuilder.App;

public partial class MergePeopleWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MergePeopleWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        PrimaryPersonCombo.ItemsSource = _viewModel.People;
        SecondaryPersonCombo.ItemsSource = _viewModel.People;

        PrimaryPersonCombo.SelectedItem = _viewModel.SelectedPerson ?? _viewModel.People.FirstOrDefault();
        SecondaryPersonCombo.SelectedItem = _viewModel.People.FirstOrDefault(person => !ReferenceEquals(person, PrimaryPersonCombo.SelectedItem));
        RefreshPreview();
    }

    private Person? PrimaryPerson => PrimaryPersonCombo.SelectedItem as Person;
    private Person? SecondaryPerson => SecondaryPersonCombo.SelectedItem as Person;

    private void PersonSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshPreview();
    }

    private void MergePeople_Click(object sender, RoutedEventArgs e)
    {
        if (PrimaryPerson is null || SecondaryPerson is null)
        {
            MessageBox.Show(this, "Choose both a primary person and a secondary person before merging.", "Merge People", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (PrimaryPerson.Id == SecondaryPerson.Id)
        {
            MessageBox.Show(this, "Choose two different people. A person cannot be merged with themselves.", "Merge People", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirmation = MessageBox.Show(
            this,
            "Merging cannot be undone unless the project was backed up or has not been saved yet. Continue with this merge?",
            "Confirm Merge",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        var success = _viewModel.TryMergePeople(PrimaryPerson, SecondaryPerson, out var message, this);
        MessageBox.Show(this, message, success ? "Merge Complete" : "Merge Not Completed", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);

        if (success)
        {
            DialogResult = true;
            Close();
        }
        else
        {
            RefreshPreview();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void RefreshPreview()
    {
        if (PreviewBox is null)
        {
            return;
        }

        PreviewBox.Text = _viewModel.BuildMergePreview(PrimaryPerson, SecondaryPerson);
    }
}
