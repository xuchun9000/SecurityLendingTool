using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SecurityLendingTool.Models;

namespace SecurityLendingTool.ViewModels;

public partial class DemoOneViewModel : ObservableObject
{
    private const int TotalRows = 100;
    private const int FilledRows = 20;
    private const int FillRangeRows = 40; // data rows are placed only within the first 40 rows

    private static readonly string[] FirstNames =
    {
        "Alice", "Bob", "Carol", "David", "Elena", "Frank", "Grace", "Henry", "Ivy", "Jack",
        "Karen", "Leo", "Maya", "Nathan", "Olivia", "Paul", "Quinn", "Rachel", "Sam", "Tina",
    };

    private static readonly string[] LastNames =
    {
        "Chen", "Martinez", "Nguyen", "Kim", "Rossi", "Patel", "Okonkwo", "Walsh", "Garcia", "Smith",
        "Johnson", "Lee", "Brown", "Wang", "Lopez", "Miller", "Davis", "Wilson", "Taylor", "Clark",
    };

    private static readonly string[] Cities =
    {
        "Seattle", "Austin", "Chicago", "Boston", "Denver", "Portland", "Atlanta", "Phoenix",
        "New York", "San Diego", "Miami", "Dallas",
    };

    [ObservableProperty] private Person? _SelectedPeople;

    // Pushed by DataGridSelectionStatsBehavior (OneWayToSource) for the selected numeric cells.
    [ObservableProperty] private int _SelectionCount;

    [ObservableProperty] private double _SelectionSum;

    [ObservableProperty] private double _SelectionAverage;

    // Pushed by DataGridSelectionStatsBehavior (OneWayToSource): every row that has a selected cell.
    [ObservableProperty] private IReadOnlyList<object> _SelectedRows = Array.Empty<object>();

    /// <summary>
    /// 100 rows: 20 rows randomly placed within the first 40 hold data, the rest are empty placeholders.
    /// </summary>
    public ObservableCollection<Person> People { get; } = CreateSampleRows();

    private static ObservableCollection<Person> CreateSampleRows()
    {
        Random random = new Random();
        ObservableCollection<Person> rows = new ObservableCollection<Person>();
        for (int i = 0; i < TotalRows; i++)
        {
            rows.Add(Person.CreateEmpty());
        }

        // Pick distinct random row positions to fill, within the first FillRangeRows rows.
        IEnumerable<int> positions = Enumerable.Range(0, FillRangeRows)
                                  .OrderBy(_ => random.Next())
                                  .Take(FilledRows);

        foreach (int index in positions)
        {
            string first = FirstNames[random.Next(FirstNames.Length)];
            string last = LastNames[random.Next(LastNames.Length)];
            rows[index] = new Person
            {
                Name = $"{first} {last}",
                Age = random.Next(22, 66),
                Score = Math.Round(60 + random.NextDouble() * 40, 2),
                City = Cities[random.Next(Cities.Length)],
                Email = $"{first.ToLowerInvariant()}.{last.ToLowerInvariant()}@example.com",
            };
        }

        return rows;
    }
}