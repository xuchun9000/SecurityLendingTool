using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SecurityLendingTool.Models;

namespace SecurityLendingTool.ViewModels;

public partial class DemoOneViewModel : ObservableObject
{
    [ObservableProperty]
    private Person? selectedPeople;

    // Pushed by DataGridSelectionStatsBehavior (OneWayToSource) for the selected numeric cells.
    [ObservableProperty]
    private int selectionCount;

    [ObservableProperty]
    private double selectionSum;

    [ObservableProperty]
    private double selectionAverage;

    public ObservableCollection<Person> People { get; } = new()
    {
        new Person { Name = "Alice Chen", Age = 28, Score = 88.5, City = "Seattle", Email = "alice.chen@example.com" },
        new Person { Name = "Bob Martinez", Age = 34, Score = 92.25, City = "Austin", Email = "bob.martinez@example.com" },
        new Person { Name = "Carol Nguyen", Age = 41, Score = 76.0, City = "Chicago", Email = "carol.nguyen@example.com" },
        new Person { Name = "David Kim", Age = 25, Score = 81.75, City = "Boston", Email = "david.kim@example.com" },
        new Person { Name = "Elena Rossi", Age = 31, Score = 95.5, City = "Denver", Email = "elena.rossi@example.com" },
        new Person { Name = "Frank Patel", Age = 47, Score = 69.25, City = "Portland", Email = "frank.patel@example.com" },
        new Person { Name = "Grace Okonkwo", Age = 29, Score = 90.0, City = "Atlanta", Email = "grace.okonkwo@example.com" },
        new Person { Name = "Henry Walsh", Age = 36, Score = 84.5, City = "Phoenix", Email = "henry.walsh@example.com" },
    };
}
