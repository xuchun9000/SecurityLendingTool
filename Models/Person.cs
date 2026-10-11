using SecurityLendingTool.Behaviors;

namespace SecurityLendingTool.Models;

public class Person : IEmptyRow
{
    public string Name { get; set; } = string.Empty;

    // Nullable so empty rows show blank cells (and are ignored by the stats behavior).
    public int? Age { get; set; }
    public double? Score { get; set; }

    public string City { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>A placeholder row with no data.</summary>
    public bool IsEmpty => string.IsNullOrEmpty(Name);

    public static Person CreateEmpty() => new();

    public override string ToString() => Name;
}