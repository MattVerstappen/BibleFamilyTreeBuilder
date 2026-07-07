using System.ComponentModel;
using System.Windows;
using BibleFamilyTreeBuilder.App.ViewModels;

namespace BibleFamilyTreeBuilder.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;

        _viewModel.CanvasRefreshRequested += TreeViewCanvas.Refresh;
        _viewModel.ResetCanvasViewRequested += TreeViewCanvas.ResetView;
        _viewModel.CenterSelectedRequested += TreeViewCanvas.CenterSelectedPerson;
        _viewModel.FitTreeToViewRequested += TreeViewCanvas.FitTreeToView;
        _viewModel.ExportCurrentViewRequested += TreeViewCanvas.ExportCurrentViewAsPng;
        _viewModel.ExportFullTreeRequested += TreeViewCanvas.ExportFullTreeAsPng;
        _viewModel.ManageGenerationsRequested += ShowGenerationLabelsManager;
        _viewModel.MergePeopleRequested += ShowMergePeopleWindow;
        _viewModel.CustomizeThemeRequested += ShowThemeEditor;
    }

    private void ShowThemeEditor()
    {
        var editor = new ThemeEditorWindow
        {
            Owner = this
        };

        editor.ShowDialog();
    }

    private void DeletePerson_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ConfirmAndDeleteSelectedPerson(this);
    }

    private void ShowGenerationLabelsManager()
    {
        var manager = new GenerationLabelsWindow(_viewModel.Project, _viewModel.NotifyGenerationLabelsChanged)
        {
            Owner = this
        };

        manager.ShowDialog();
    }

    private void ShowMergePeopleWindow()
    {
        var mergeWindow = new MergePeopleWindow(_viewModel)
        {
            Owner = this
        };

        mergeWindow.ShowDialog();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (!_viewModel.ConfirmContinueWithoutSaving(this))
        {
            e.Cancel = true;
        }
    }
}
