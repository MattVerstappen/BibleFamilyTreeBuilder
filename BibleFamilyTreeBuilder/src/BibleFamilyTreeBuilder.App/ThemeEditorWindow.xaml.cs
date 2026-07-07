using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using BibleFamilyTreeBuilder.App.Theming;

namespace BibleFamilyTreeBuilder.App;

public partial class ThemeEditorWindow : Window
{
    // The curated set of colors exposed in the editor. Anything not listed here inherits
    // from the chosen base preset. Some rows write more than one palette field.
    private static readonly (string Label, Func<ThemePalette, Color> Get, Action<ThemePalette, Color> Set)[] Descriptors =
    [
        ("Window background", p => p.WindowBackground, (p, c) => p.WindowBackground = c),
        ("Panel background", p => p.PanelBackground, (p, c) => p.PanelBackground = c),
        ("Panel border", p => p.PanelBorder, (p, c) => p.PanelBorder = c),
        ("Toolbar & menu", p => p.ToolBarBackground, (p, c) => { p.ToolBarBackground = c; p.MenuBackground = c; }),
        ("Inset box background", p => p.InsetBackground, (p, c) => p.InsetBackground = c),
        ("Input background", p => p.ControlBackground, (p, c) => p.ControlBackground = c),
        ("Input border", p => p.ControlBorder, (p, c) => p.ControlBorder = c),
        ("Primary text", p => p.TextPrimary, (p, c) => { p.TextPrimary = c; p.CardTitleText = c; }),
        ("Secondary text", p => p.TextSecondary, (p, c) => p.TextSecondary = c),
        ("Accent", p => p.Accent, (p, c) => p.Accent = c),
        ("Canvas background", p => p.CanvasBackground, (p, c) => p.CanvasBackground = c),
        ("Generation band", p => p.BandEven, (p, c) => p.BandEven = c),
        ("Default card fill", p => p.CardDefaultFill, (p, c) => p.CardDefaultFill = c),
        ("Marriage line", p => p.MarriageLine, (p, c) => p.MarriageLine = c),
        ("Selected-card border", p => p.SelectedCardBorder, (p, c) => p.SelectedCardBorder = c),
    ];

    private readonly AppTheme _original;
    private ThemePalette _working;
    private bool _saved;
    private bool _suppressBaseChange;

    public ThemeEditorWindow()
    {
        InitializeComponent();

        _original = ThemeManager.Current;
        _working = ThemeManager.GetEditableStartingPalette();

        _suppressBaseChange = true;
        BaseCombo.Items.Add(ThemePresets.Parchment);
        BaseCombo.Items.Add(ThemePresets.Light);
        BaseCombo.Items.Add(ThemePresets.Dark);
        BaseCombo.Items.Add("Current colors");
        BaseCombo.SelectedIndex = 3;
        _suppressBaseChange = false;

        RefreshRows();
    }

    private ThemePalette GetBasePalette(int index)
    {
        return index switch
        {
            0 => ThemePresets.BuiltIn[0].Palette.Clone(),
            1 => ThemePresets.BuiltIn[1].Palette.Clone(),
            2 => ThemePresets.BuiltIn[2].Palette.Clone(),
            _ => _original.Palette.Clone(),
        };
    }

    private void RefreshRows()
    {
        var items = new List<ColorEditItem>();
        foreach (var descriptor in Descriptors)
        {
            var setter = descriptor.Set;
            var item = new ColorEditItem(descriptor.Label, descriptor.Get(_working), color =>
            {
                setter(_working, color);
                ThemeManager.PreviewCustom(_working);
            });
            items.Add(item);
        }

        ColorList.ItemsSource = items;
    }

    private void BaseCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_suppressBaseChange)
        {
            return;
        }

        _working = GetBasePalette(BaseCombo.SelectedIndex);
        RefreshRows();
        ThemeManager.PreviewCustom(_working);
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _working = GetBasePalette(BaseCombo.SelectedIndex);
        RefreshRows();
        ThemeManager.PreviewCustom(_working);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ThemeManager.SaveCustom(_working);
        _saved = true;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // If the user did not save, revert to whatever theme was active when the dialog opened.
        if (!_saved)
        {
            ThemeManager.Apply(_original, persist: false);
        }
    }

    private sealed class ColorEditItem : INotifyPropertyChanged
    {
        private readonly Action<Color> _onChanged;
        private bool _updating;
        private int _r;
        private int _g;
        private int _b;
        private string _hex = "#000000";
        private SolidColorBrush _swatchBrush = new(Colors.Black);

        public ColorEditItem(string label, Color color, Action<Color> onChanged)
        {
            Label = label;
            _onChanged = onChanged;
            SetFromColor(color, notify: false);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Label { get; }

        public int R
        {
            get => _r;
            set => SetChannel(ref _r, value);
        }

        public int G
        {
            get => _g;
            set => SetChannel(ref _g, value);
        }

        public int B
        {
            get => _b;
            set => SetChannel(ref _b, value);
        }

        public string Hex
        {
            get => _hex;
            set
            {
                _hex = value;
                if (!_updating && ThemePalette.TryParseHex(value, out var color))
                {
                    SetFromColor(color, notify: true, skipHex: true);
                }

                OnPropertyChanged();
            }
        }

        public SolidColorBrush SwatchBrush
        {
            get => _swatchBrush;
            private set
            {
                _swatchBrush = value;
                OnPropertyChanged();
            }
        }

        private void SetChannel(ref int field, int value)
        {
            var clamped = Math.Clamp(value, 0, 255);
            if (field == clamped)
            {
                return;
            }

            field = clamped;
            OnPropertyChanged(nameof(R));
            OnPropertyChanged(nameof(G));
            OnPropertyChanged(nameof(B));

            if (!_updating)
            {
                PushColor();
            }
        }

        private void SetFromColor(Color color, bool notify, bool skipHex = false)
        {
            _updating = true;
            _r = color.R;
            _g = color.G;
            _b = color.B;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            SwatchBrush = brush;
            if (!skipHex)
            {
                _hex = ThemePalette.ToHex(color);
                OnPropertyChanged(nameof(Hex));
            }

            OnPropertyChanged(nameof(R));
            OnPropertyChanged(nameof(G));
            OnPropertyChanged(nameof(B));
            _updating = false;

            if (notify)
            {
                _onChanged(color);
            }
        }

        private void PushColor()
        {
            var color = Color.FromRgb((byte)_r, (byte)_g, (byte)_b);
            _updating = true;
            _hex = ThemePalette.ToHex(color);
            OnPropertyChanged(nameof(Hex));
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            SwatchBrush = brush;
            _updating = false;

            _onChanged(color);
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
