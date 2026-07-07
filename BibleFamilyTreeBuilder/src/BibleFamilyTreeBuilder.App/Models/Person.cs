using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BibleFamilyTreeBuilder.App.Models;

public class Person : INotifyPropertyChanged
{
    private string _name = "New person";
    private string _displayName = "";
    private CardType _cardType = CardType.Default;
    private string _notes = "";
    private int? _generationOverride;
    private double _manualXOffset;
    private double _manualYOffset;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetField(ref _displayName, value);
    }

    public List<string> AlsoKnownAs { get; set; } = [];

    public List<PersonNameVariant> NameVariants { get; set; } = [];

    public CardType CardType
    {
        get => _cardType;
        set => SetField(ref _cardType, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetField(ref _notes, value);
    }

    public List<string> BibleReferences { get; set; } = [];

    public int? GenerationOverride
    {
        get => _generationOverride;
        set => SetField(ref _generationOverride, value);
    }

    public double ManualXOffset
    {
        get => _manualXOffset;
        set => SetField(ref _manualXOffset, value);
    }

    public double ManualYOffset
    {
        get => _manualYOffset;
        set => SetField(ref _manualYOffset, value);
    }

    public string EffectiveDisplayName => string.IsNullOrWhiteSpace(DisplayName) ? Name : DisplayName;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        if (propertyName is nameof(Name) or nameof(DisplayName))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EffectiveDisplayName)));
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
