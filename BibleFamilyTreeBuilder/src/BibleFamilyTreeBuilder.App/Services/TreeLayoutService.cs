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

        // Children are placed under their parents: each person's preferred X is the
        // average of their parents' card centers in the rows above, and spouses are
        // kept next to each other instead of scattering alphabetically.
        var placedCenters = new Dictionary<string, double>();
        var parentsByChild = project.Relationships
            .Where(r => r.Type == RelationshipType.ParentChild)
            .ToLookup(r => r.ToPersonId, r => r.FromPersonId);
        var marriages = project.Relationships
            .Where(r => r.Type == RelationshipType.Marriage)
            .ToList();

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            var rowPeople = row
                .OrderBy(person => person.EffectiveDisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(person => person.Id)
                .ToList();
            var rowIds = rowPeople.Select(person => person.Id).ToHashSet();

            var desiredCenters = new Dictionary<string, double>();
            foreach (var person in rowPeople)
            {
                var parentCenters = parentsByChild[person.Id]
                    .Where(placedCenters.ContainsKey)
                    .Select(parentId => placedCenters[parentId])
                    .ToList();

                if (parentCenters.Count > 0)
                {
                    desiredCenters[person.Id] = parentCenters.Average();
                }
            }

            // A spouse without parents in the tree should sit right beside their
            // partner rather than at the start of the row.
            var spousePulled = true;
            while (spousePulled)
            {
                spousePulled = false;
                foreach (var marriage in marriages)
                {
                    if (!rowIds.Contains(marriage.FromPersonId) || !rowIds.Contains(marriage.ToPersonId))
                    {
                        continue;
                    }

                    if (desiredCenters.ContainsKey(marriage.FromPersonId) && !desiredCenters.ContainsKey(marriage.ToPersonId))
                    {
                        desiredCenters[marriage.ToPersonId] = desiredCenters[marriage.FromPersonId] + 1;
                        spousePulled = true;
                    }
                    else if (desiredCenters.ContainsKey(marriage.ToPersonId) && !desiredCenters.ContainsKey(marriage.FromPersonId))
                    {
                        desiredCenters[marriage.FromPersonId] = desiredCenters[marriage.ToPersonId] + 1;
                        spousePulled = true;
                    }
                }
            }

            var orderedPeople = rowPeople
                .OrderBy(person => desiredCenters.TryGetValue(person.Id, out var center) ? center : double.MaxValue)
                .ThenBy(person => person.EffectiveDisplayName, StringComparer.OrdinalIgnoreCase)
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

            var cursor = LeftPadding;
            var y = TopPadding + rowIndex * (CardHeight + VerticalGap);

            foreach (var person in orderedPeople)
            {
                var x = desiredCenters.TryGetValue(person.Id, out var desiredCenter)
                    ? Math.Max(cursor, desiredCenter - CardWidth / 2)
                    : cursor;
                cursor = x + CardWidth + HorizontalGap;

                // Manual nudges are final offsets. Descendants follow the nudged
                // position, so dragging a parent keeps their children underneath.
                var drawX = x + person.ManualXOffset;
                var drawY = y + person.ManualYOffset;
                placedCenters[person.Id] = drawX + CardWidth / 2;

                result.PersonBounds[person.Id] = new Rect(drawX, drawY, CardWidth, CardHeight);
                maxWidth = Math.Max(maxWidth, drawX + CardWidth + LeftPadding);
            }
        }

        var maxBottom = result.PersonBounds.Values.DefaultIfEmpty(new Rect(0, 0, 400, 300)).Max(rect => rect.Bottom);
        result.Width = Math.Max(900, maxWidth);
        result.Height = Math.Max(620, maxBottom + TopPadding + 40);

        return result;
    }

    private static Dictionary<string, int> CalculateGenerations(TreeProject project)
    {
        var generations = new Dictionary<string, int>();

        // Manual overrides always win for the person who set them. Everyone whose
        // generation is derived from an override or a parent counts as "anchored";
        // a spouse without an anchor is pulled onto their partner's row instead of
        // defaulting to the top of the tree.
        var overridden = new HashSet<string>();
        var anchored = new HashSet<string>();

        foreach (var person in project.People)
        {
            if (person.GenerationOverride.HasValue)
            {
                generations[person.Id] = person.GenerationOverride.Value;
                overridden.Add(person.Id);
                anchored.Add(person.Id);
            }
            else
            {
                generations[person.Id] = 0;
            }
        }

        var parentChildRelationships = project.Relationships
            .Where(r => r.Type == RelationshipType.ParentChild)
            .ToList();
        var marriages = project.Relationships
            .Where(r => r.Type == RelationshipType.Marriage)
            .ToList();

        foreach (var relationship in parentChildRelationships)
        {
            if (generations.ContainsKey(relationship.FromPersonId) && generations.ContainsKey(relationship.ToPersonId))
            {
                anchored.Add(relationship.ToPersonId);
            }
        }

        // Deterministic relaxation: every parent-child edge pushes the child at least
        // one row below the parent (unless the child has a manual override), and every
        // marriage copies a known generation onto an unanchored spouse. Repeating the
        // pass handles chains such as Abraham -> Isaac -> Jacob plus their spouses.
        var maxPasses = project.People.Count + project.Relationships.Count + 1;
        for (var pass = 0; pass < maxPasses; pass++)
        {
            var changed = false;

            foreach (var relationship in parentChildRelationships)
            {
                if (!generations.TryGetValue(relationship.FromPersonId, out var parentGeneration) ||
                    !generations.TryGetValue(relationship.ToPersonId, out var childGeneration))
                {
                    continue;
                }

                if (overridden.Contains(relationship.ToPersonId))
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

            foreach (var marriage in marriages)
            {
                if (!generations.TryGetValue(marriage.FromPersonId, out var fromGeneration) ||
                    !generations.TryGetValue(marriage.ToPersonId, out var toGeneration))
                {
                    continue;
                }

                var fromAnchored = anchored.Contains(marriage.FromPersonId);
                var toAnchored = anchored.Contains(marriage.ToPersonId);

                if (fromAnchored && !toAnchored)
                {
                    if (toGeneration != fromGeneration)
                    {
                        generations[marriage.ToPersonId] = fromGeneration;
                        changed = true;
                    }

                    if (anchored.Add(marriage.ToPersonId))
                    {
                        changed = true;
                    }
                }
                else if (toAnchored && !fromAnchored)
                {
                    if (fromGeneration != toGeneration)
                    {
                        generations[marriage.FromPersonId] = toGeneration;
                        changed = true;
                    }

                    if (anchored.Add(marriage.FromPersonId))
                    {
                        changed = true;
                    }
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
