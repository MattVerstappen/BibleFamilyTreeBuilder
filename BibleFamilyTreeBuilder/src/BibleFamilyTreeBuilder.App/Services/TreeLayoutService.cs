using System.Windows;
using BibleFamilyTreeBuilder.App.Models;

namespace BibleFamilyTreeBuilder.App.Services;

public class TreeLayoutResult
{
    public Dictionary<string, Rect> PersonBounds { get; } = [];
    public Dictionary<string, int> PersonGenerations { get; } = [];
    public List<GenerationLane> GenerationLanes { get; } = [];
    public double Width { get; set; }
    public double Height { get; set; }
}

public class GenerationLane
{
    public int Generation { get; set; }
    public string Title { get; set; } = "";
    public double Top { get; set; }
    public double Height { get; set; }
    public string DisplayName => string.IsNullOrWhiteSpace(Title)
        ? $"Generation {Generation}"
        : $"Generation {Generation} - {Title}";
}

public class TreeLayoutService
{
    public const double CardWidth = 168;
    public const double CardHeight = 86;
    private const double HorizontalGap = 54;
    private const double VerticalGap = 104;
    private const double LeftPadding = 286;
    private const double TopPadding = 84;

    public TreeLayoutResult Layout(TreeProject project)
    {
        var generations = CalculateGenerations(project);

        // Spouses usually belong on the same timeline row. This simple pass copies a known
        // generation from one spouse to the other when the other spouse has not been placed yet.
        foreach (var marriage in project.Relationships.Where(r => r.Type == RelationshipType.Marriage))
        {
            if (!generations.TryGetValue(marriage.FromPersonId, out var fromGeneration))
            {
                fromGeneration = 0;
            }

            if (!generations.TryGetValue(marriage.ToPersonId, out var toGeneration))
            {
                toGeneration = fromGeneration;
            }

            var generation = Math.Min(fromGeneration, toGeneration);
            generations[marriage.FromPersonId] = generation;
            generations[marriage.ToPersonId] = generation;
        }

        // Manual generation overrides are applied after relationship-based generations.
        // This lets the user place someone on the same broad time row as David, Moses,
        // or another biblical figure even when the exact ancestor chain is incomplete.
        foreach (var person in project.People)
        {
            if (person.GenerationOverride.HasValue)
            {
                generations[person.Id] = person.GenerationOverride.Value;
            }
            else if (!generations.ContainsKey(person.Id))
            {
                generations[person.Id] = 0;
            }
        }

        var result = new TreeLayoutResult();
        foreach (var generation in generations)
        {
            result.PersonGenerations[generation.Key] = generation.Value;
        }

        var rows = project.People
            .GroupBy(person => generations[person.Id])
            .OrderBy(group => group.Key)
            .ToList();

        var maxWidth = 0.0;
        var generationLabels = project.GenerationLabels
            .GroupBy(label => label.GenerationNumber)
            .ToDictionary(group => group.Key, group => group.First().Title);

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            var orderedPeople = row
                .OrderBy(person => person.EffectiveDisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(person => person.Id)
                .ToList();

            var laneTop = TopPadding - 26 + rowIndex * (CardHeight + VerticalGap);
            result.GenerationLanes.Add(new GenerationLane
            {
                Generation = row.Key,
                Title = generationLabels.TryGetValue(row.Key, out var title) ? title : "",
                Top = laneTop,
                Height = CardHeight + 52
            });

            for (var index = 0; index < orderedPeople.Count; index++)
            {
                var person = orderedPeople[index];
                var x = LeftPadding + index * (CardWidth + HorizontalGap);
                var y = TopPadding + rowIndex * (CardHeight + VerticalGap);

                // Manual nudges are final offsets. They do not change the underlying
                // generation calculation, so Auto Layout remains predictable.
                x += person.ManualXOffset;
                y += person.ManualYOffset;

                result.PersonBounds[person.Id] = new Rect(x, y, CardWidth, CardHeight);
                maxWidth = Math.Max(maxWidth, x + CardWidth + LeftPadding);
            }
        }

        var maxBottom = result.PersonBounds.Values.DefaultIfEmpty(new Rect(0, 0, 400, 300)).Max(rect => rect.Bottom);
        result.Width = Math.Max(900, maxWidth);
        result.Height = Math.Max(620, maxBottom + TopPadding + 40);

        return result;
    }

    private static Dictionary<string, int> CalculateGenerations(TreeProject project)
    {
        var generations = project.People.ToDictionary(person => person.Id, _ => 0);
        var parentChildRelationships = project.Relationships
            .Where(r => r.Type == RelationshipType.ParentChild)
            .ToList();

        // Deterministic relaxation: every parent-child edge pushes the child at least
        // one row below the parent. Repeating the pass handles chains such as Abraham
        // to Isaac to Jacob without needing a complex graph algorithm.
        for (var pass = 0; pass < project.People.Count; pass++)
        {
            var changed = false;

            foreach (var relationship in parentChildRelationships)
            {
                if (!generations.TryGetValue(relationship.FromPersonId, out var parentGeneration))
                {
                    continue;
                }

                if (!generations.TryGetValue(relationship.ToPersonId, out var childGeneration))
                {
                    continue;
                }

                var proposedChildGeneration = parentGeneration + 1;
                if (childGeneration < proposedChildGeneration)
                {
                    generations[relationship.ToPersonId] = proposedChildGeneration;
                    changed = true;
                }
            }

            if (!changed)
            {
                break;
            }
        }

        return generations;
    }
}
