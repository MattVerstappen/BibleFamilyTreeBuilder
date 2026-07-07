using BibleFamilyTreeBuilder.App.Models;

namespace BibleFamilyTreeBuilder.App.Services;

public static class SampleProjectFactory
{
    public static TreeProject Create()
    {
        var project = new TreeProject { Name = "Bible Family Tree" };

        var jephunneh = Person("Jephunneh", notes: "Father of Caleb.");
        var kenaz = Person("Kenaz", references: ["Joshua 15:17", "Judges 1:13"]);
        var caleb = Person(
            "Caleb",
            aliases: ["Caleb son of Jephunneh", "Caleb the Kenizzite"],
            references: ["Numbers 13:6", "Joshua 14:6-14"],
            notes: "Included to show biblical references and alternate names.");
        var othniel = Person("Othniel", references: ["Joshua 15:17", "Judges 3:9"]);
        var achsah = Person("Achsah", references: ["Joshua 15:16-19"]);
        var unknown = Person("UNKNOWN", CardType.Unknown, notes: "A placeholder where the person is not named in the source.");
        var unknownDescendants = Person("Unknown descendants", CardType.UnknownDescendant, notes: "A blue person-style card that represents a missing descendant chain.");
        var wives = Person("700 wives", CardType.GroupedPeople, notes: "A grouped-people card for cases where scripture names a group rather than each person.");
        var joseph = Person("Joseph", CardType.JesusLine, references: ["Matthew 1:16", "Luke 2:4"]);
        var mary = Person("Mary", CardType.JesusLine, aliases: ["Miriam"], references: ["Luke 1:26-38", "Matthew 1:18"]);
        mary.NameVariants.Add(new PersonNameVariant
        {
            NameUsed = "Miriam",
            Book = "Luke",
            Reference = "Luke 1:27",
            Notes = "Sample variant showing a source-specific form of Mary's name."
        });

        var jesus = Person("Jesus", CardType.JesusLine, references: ["Matthew 1:1", "Luke 3:23-38"], notes: "Marked as Jesus Line by the user in the sample.");

        foreach (var person in new[] { jephunneh, kenaz, caleb, othniel, achsah, unknown, unknownDescendants, wives, joseph, mary, jesus })
        {
            project.People.Add(person);
        }

        AddParentChild(project, jephunneh, caleb, ParentKind.Biological);
        AddParentChild(project, kenaz, othniel, ParentKind.Unknown);
        AddParentChild(project, caleb, achsah, ParentKind.Biological);
        AddParentChild(project, unknown, unknownDescendants, ParentKind.Unknown);
        AddParentChild(project, joseph, jesus, ParentKind.Legal, "Adopted/legal father", "Legal father", ["Matthew 1:16"], "Joseph is named in Matthew's genealogy and appears as Jesus' legal father.", EvidenceLevel.Direct);
        AddParentChild(project, mary, jesus, ParentKind.Biological, "Mother", "Mother", ["Luke 1:31-35"], "Mary is directly connected to Jesus' birth narrative.", EvidenceLevel.Direct);
        AddMarriage(project, joseph, mary, "Marriage", "Betrothed", ["Matthew 1:18"], "Matthew describes Mary as pledged to Joseph.", EvidenceLevel.Direct);
        AddMarriage(project, unknown, wives, "Grouped marriage");

        return project;
    }

    private static Person Person(
        string name,
        CardType cardType = CardType.Default,
        List<string>? aliases = null,
        List<string>? references = null,
        string notes = "")
    {
        return new Person
        {
            Name = name,
            DisplayName = name,
            CardType = cardType,
            AlsoKnownAs = aliases ?? [],
            BibleReferences = references ?? [],
            Notes = notes
        };
    }

    private static void AddParentChild(
        TreeProject project,
        Person parent,
        Person child,
        ParentKind parentKind,
        string label = "",
        string displayLabel = "",
        List<string>? bibleReferences = null,
        string notes = "",
        EvidenceLevel evidenceLevel = EvidenceLevel.Unknown)
    {
        project.Relationships.Add(new Relationship
        {
            Type = RelationshipType.ParentChild,
            FromPersonId = parent.Id,
            ToPersonId = child.Id,
            ParentKind = parentKind,
            Label = label,
            DisplayLabel = displayLabel,
            BibleReferences = bibleReferences ?? [],
            Notes = notes,
            EvidenceLevel = evidenceLevel
        });
    }

    private static void AddMarriage(
        TreeProject project,
        Person spouseA,
        Person spouseB,
        string label = "Marriage",
        string displayLabel = "",
        List<string>? bibleReferences = null,
        string notes = "",
        EvidenceLevel evidenceLevel = EvidenceLevel.Unknown)
    {
        project.Relationships.Add(new Relationship
        {
            Type = RelationshipType.Marriage,
            FromPersonId = spouseA.Id,
            ToPersonId = spouseB.Id,
            Label = label,
            DisplayLabel = displayLabel,
            BibleReferences = bibleReferences ?? [],
            Notes = notes,
            EvidenceLevel = evidenceLevel
        });
    }
}
