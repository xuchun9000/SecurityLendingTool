using System.ComponentModel;
using System.Reflection;

namespace SecurityLendingTool.Tasks.Core;

/// <summary>What happened in a <see cref="ISLTask.ProgressChanged"/> report.</summary>
public enum SLTaskProgressEvent
{
    [Description("Precondition not met, skipped")]
    PreconditionNotMet,

    [Description("Started")]
    Started,

    [Description("In progress")]
    Progress,

    [Description("Completed")]
    Completed,

    [Description("Failed")]
    Failed,

    [Description("Canceled")]
    Canceled,
}

public static class SLTaskProgressEventExtensions
{
    /// <summary>Returns the [Description] text of the value, or its name if it has none.</summary>
    public static string ToDescription(this SLTaskProgressEvent value)
    {
        string name = value.ToString();
        FieldInfo? field = typeof(SLTaskProgressEvent).GetField(name);
        DescriptionAttribute? attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        if (attribute == null)
        {
            return name;
        }

        return attribute.Description;
    }
}