using System.Windows;
using BibleFamilyTreeBuilder.App.Theming;

namespace BibleFamilyTreeBuilder.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Load and apply the saved theme before the main window is shown.
        ThemeManager.Initialize();
    }
}
