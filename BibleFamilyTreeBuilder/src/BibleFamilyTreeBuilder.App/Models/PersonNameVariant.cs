namespace BibleFamilyTreeBuilder.App.Models;

public class PersonNameVariant
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string NameUsed { get; set; } = "";
    public string Book { get; set; } = "";
    public string Reference { get; set; } = "";
    public string Notes { get; set; } = "";
    public string TranslationOrSource { get; set; } = "";
}
