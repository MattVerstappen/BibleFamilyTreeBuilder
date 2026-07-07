namespace BibleFamilyTreeBuilder.App.Models;

public class Relationship
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public RelationshipType Type { get; set; }
    public string FromPersonId { get; set; } = "";
    public string ToPersonId { get; set; } = "";
    public string Label { get; set; } = "";
    public string DisplayLabel { get; set; } = "";
    public string Notes { get; set; } = "";
    public List<string> BibleReferences { get; set; } = [];
    public EvidenceLevel EvidenceLevel { get; set; } = EvidenceLevel.Unknown;
    public ParentKind ParentKind { get; set; } = ParentKind.Unknown;
}
