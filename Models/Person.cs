namespace SecurityLendingTool.Models;

public class Person
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public double Score { get; set; }
    public string City { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public override string ToString() => Name;
}
