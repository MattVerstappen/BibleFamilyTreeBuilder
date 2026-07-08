using System.IO;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using BibleFamilyTreeBuilder.App.Models;

namespace BibleFamilyTreeBuilder.App.Services;

public class ProjectJsonService
{
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task SaveAsync(TreeProject project, string filePath)
    {
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, project, _options);
    }

    // Synchronous save for callers already on the UI thread (e.g. automatic backups
    // before destructive actions). Blocking on SaveAsync there deadlocks the app.
    public void Save(TreeProject project, string filePath)
    {
        using var stream = File.Create(filePath);
        JsonSerializer.Serialize(stream, project, _options);
    }

    public async Task<TreeProject> LoadAsync(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        var project = await JsonSerializer.DeserializeAsync<TreeProject>(stream, _options);
        return Normalize(project ?? new TreeProject());
    }

    private static TreeProject Normalize(TreeProject project)
    {
        project.People ??= new ObservableCollection<Person>();
        project.Relationships ??= new ObservableCollection<Relationship>();
        project.GenerationLabels ??= new ObservableCollection<GenerationLabel>();
        project.People = new ObservableCollection<Person>(project.People.Where(person => person is not null)!);
        project.Relationships = new ObservableCollection<Relationship>(project.Relationships.Where(relationship => relationship is not null)!);
        project.GenerationLabels = new ObservableCollection<GenerationLabel>(project.GenerationLabels.Where(label => label is not null)!);

        foreach (var person in project.People)
        {
            person.Id = string.IsNullOrWhiteSpace(person.Id) ? Guid.NewGuid().ToString("N") : person.Id;
            person.Name = string.IsNullOrWhiteSpace(person.Name) ? "Unnamed person" : person.Name;
            person.AlsoKnownAs ??= [];
            person.BibleReferences ??= [];
            person.NameVariants ??= [];
            person.NameVariants = person.NameVariants.Where(variant => variant is not null).ToList()!;

            foreach (var variant in person.NameVariants)
            {
                variant.Id = string.IsNullOrWhiteSpace(variant.Id) ? Guid.NewGuid().ToString("N") : variant.Id;
                variant.NameUsed ??= "";
                variant.Book ??= "";
                variant.Reference ??= "";
                variant.Notes ??= "";
                variant.TranslationOrSource ??= "";
            }
        }

        foreach (var relationship in project.Relationships)
        {
            relationship.Id = string.IsNullOrWhiteSpace(relationship.Id) ? Guid.NewGuid().ToString("N") : relationship.Id;
            relationship.Label ??= "";
            relationship.DisplayLabel ??= "";
            relationship.Notes ??= "";
            relationship.BibleReferences ??= [];
        }

        foreach (var generationLabel in project.GenerationLabels)
        {
            generationLabel.Title ??= "";
            generationLabel.Notes ??= "";
        }

        return project;
    }
}
