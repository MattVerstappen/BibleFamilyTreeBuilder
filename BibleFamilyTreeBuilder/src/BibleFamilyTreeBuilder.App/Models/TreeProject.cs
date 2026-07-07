using System.Collections.ObjectModel;

namespace BibleFamilyTreeBuilder.App.Models;

public class TreeProject
{
    public string Name { get; set; } = "Bible Family Tree";
    public ObservableCollection<Person> People { get; set; } = [];
    public ObservableCollection<Relationship> Relationships { get; set; } = [];
    public ObservableCollection<GenerationLabel> GenerationLabels { get; set; } = [];
}
