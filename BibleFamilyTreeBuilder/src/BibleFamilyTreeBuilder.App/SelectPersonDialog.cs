using System.Windows;
using System.Windows.Controls;
using BibleFamilyTreeBuilder.App.Models;

namespace BibleFamilyTreeBuilder.App;

// Small picker used when an action needs one person chosen from a short list,
// e.g. choosing which spouse is the other parent of a new child.
public class SelectPersonDialog : Window
{
    private readonly ListBox _listBox;
    private Person? _selectedPerson;

    private SelectPersonDialog(string title, string prompt, IReadOnlyList<Person> people)
    {
        Title = title;
        Width = 380;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;

        var promptText = new TextBlock
        {
            Text = prompt,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };

        _listBox = new ListBox
        {
            MaxHeight = 240,
            Margin = new Thickness(0, 0, 0, 12),
            ItemsSource = people,
            DisplayMemberPath = nameof(Person.EffectiveDisplayName),
            SelectedIndex = people.Count > 0 ? 0 : -1
        };
        _listBox.MouseDoubleClick += (_, _) => Accept();

        var okButton = new Button
        {
            Content = "OK",
            Width = 90,
            Margin = new Thickness(0, 0, 8, 0),
            IsDefault = true
        };
        okButton.Click += (_, _) => Accept();

        var skipButton = new Button
        {
            Content = "No second parent",
            Width = 130,
            IsCancel = true
        };

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        buttonRow.Children.Add(okButton);
        buttonRow.Children.Add(skipButton);

        var rootPanel = new StackPanel
        {
            Margin = new Thickness(16)
        };
        rootPanel.Children.Add(promptText);
        rootPanel.Children.Add(_listBox);
        rootPanel.Children.Add(buttonRow);

        Content = rootPanel;
    }

    private void Accept()
    {
        if (_listBox.SelectedItem is Person person)
        {
            _selectedPerson = person;
            DialogResult = true;
        }
    }

    public static Person? Show(Window? owner, string title, string prompt, IReadOnlyList<Person> people)
    {
        var dialog = new SelectPersonDialog(title, prompt, people);
        if (owner is not null)
        {
            dialog.Owner = owner;
        }

        return dialog.ShowDialog() == true ? dialog._selectedPerson : null;
    }
}
