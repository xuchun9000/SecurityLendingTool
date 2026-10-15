using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using SecurityLendingTool.Services;

namespace SecurityLendingTool.ViewModels;

/// <summary>Level of a node in the Message History tree.</summary>
public enum MessageHistoryNodeKind
{
    [Description("Year")]
    Year,

    [Description("Month")]
    Month,

    [Description("Day")]
    Day,
}

/// <summary>How a row of the Message History grid is drawn.</summary>
public enum MessageHistoryRowKind
{
    [Description("Summary of an SLTask that ended Completed")]
    SummaryCompleted,

    [Description("Summary of an SLTask that ended Failed or Canceled")]
    SummaryFailed,

    [Description("Summary of an SLTask that was skipped")]
    SummarySkipped,

    [Description("Summary of an SLTask that is still running or never finished")]
    SummaryOther,

    [Description("One message")]
    Message,

    [Description("One Failed message")]
    FailedMessage,
}

/// <summary>A year, month or day in the Message History tree.</summary>
public partial class MessageHistoryTreeNode : ObservableObject
{
    public MessageHistoryTreeNode(MessageHistoryNodeKind kind, string header, DateOnly date, int notCompletedCount)
    {
        Kind = kind;
        Header = header;
        Date = date;
        NotCompletedCount = notCompletedCount;
        bool isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        if (kind != MessageHistoryNodeKind.Day)
        {
            Foreground = "Black";
        }
        else
        {
            Foreground = isWeekend ? "Gray" : "Green";
        }

        FailedText = notCompletedCount > 0 ? $"● {notCompletedCount} failed" : "";
    }

    public MessageHistoryNodeKind Kind { get; }

    public string Header { get; }

    public DateOnly Date { get; }

    public int NotCompletedCount { get; }

    /// <summary>Green for Mon - Fri, gray for Sat / Sun, black for years and months.</summary>
    public string Foreground { get; }

    /// <summary>"● N failed" for a day where N SLTasks did not end Completed; empty otherwise.</summary>
    public string FailedText { get; }

    public ObservableCollection<MessageHistoryTreeNode> Children { get; } = new ObservableCollection<MessageHistoryTreeNode>();

    [ObservableProperty] private bool _IsExpanded;

    [ObservableProperty] private bool _IsSelected;
}

/// <summary>One stored message with its attempt number (1-based) within the SLTask's day.</summary>
public sealed record MessageHistoryEntry(SLTaskStoredMessage Message, int Attempt);

/// <summary>All messages of one SLTask on one day.</summary>
public sealed class MessageHistorySLTaskGroup
{
    public MessageHistorySLTaskGroup(string slTaskName, IReadOnlyList<MessageHistoryEntry> entries)
    {
        SLTaskName = slTaskName;
        Entries = entries;
        AttemptCount = entries.Count == 0 ? 0 : entries[^1].Attempt;
        FinalStatus = entries.Count == 0 ? "" : entries[^1].Message.Status;
        HasFailure = entries.Any(entry => entry.Message.Event == "Failed");
        FirstTime = entries.Count == 0 ? DateTime.MinValue : entries[0].Message.Timestamp;
        Summary = BuildSummary();
        IsExpanded = HasFailure || FinalStatus != "Completed";
    }

    public string SLTaskName { get; }

    public IReadOnlyList<MessageHistoryEntry> Entries { get; }

    public int AttemptCount { get; }

    public string FinalStatus { get; }

    public bool HasFailure { get; }

    public DateTime FirstTime { get; }

    public string Summary { get; }

    public bool IsExpanded { get; set; }

    public MessageHistoryRowKind SummaryKind => FinalStatus switch
    {
        "Completed" => MessageHistoryRowKind.SummaryCompleted,
        "Failed" or "Canceled" => MessageHistoryRowKind.SummaryFailed,
        "Skipped" => MessageHistoryRowKind.SummarySkipped,
        _ => MessageHistoryRowKind.SummaryOther,
    };

    private string BuildSummary()
    {
        if (Entries.Count == 0)
        {
            return "";
        }

        string attempts = AttemptCount == 1 ? "1 attempt" : $"{AttemptCount} attempts";
        switch (FinalStatus)
        {
            case "Completed":
            {
                if (AttemptCount == 1)
                {
                    return string.Create(CultureInfo.InvariantCulture, $"{Entries[0].Message.Timestamp:HH:mm:ss} – {Entries[^1].Message.Timestamp:HH:mm:ss}");
                }

                int failedCount = AttemptCount - 1;
                string times = failedCount == 1 ? "once" : $"{failedCount} times";
                return $"Failed {times}, completed on attempt {AttemptCount}";
            }

            case "Failed":
            {
                return $"Still failing after {attempts}";
            }

            case "Skipped":
            case "Canceled":
            {
                return Entries[^1].Message.Message;
            }

            default:
            {
                return $"{FinalStatus} (attempt {AttemptCount})";
            }
        }
    }
}

/// <summary>A row of the Message History grid: an SLTask summary row or one of its messages.</summary>
public partial class MessageHistoryRow : ObservableObject
{
    private MessageHistoryRow(MessageHistorySLTaskGroup group, MessageHistoryEntry? entry, bool isNewAttempt)
    {
        Group = group;
        Entry = entry;
        IsNewAttempt = isNewAttempt;
    }

    public static MessageHistoryRow ForSummary(MessageHistorySLTaskGroup group)
    {
        MessageHistoryRow row = new MessageHistoryRow(group, null, false);
        row.UpdateGlyph();
        return row;
    }

    public static MessageHistoryRow ForEntry(MessageHistorySLTaskGroup group, MessageHistoryEntry entry, bool isNewAttempt)
    {
        return new MessageHistoryRow(group, entry, isNewAttempt);
    }

    public MessageHistorySLTaskGroup Group { get; }

    /// <summary>The message of a detail row; null for a summary row.</summary>
    public MessageHistoryEntry? Entry { get; }

    public bool IsSummary => Entry == null;

    /// <summary>True for the first shown message of an attempt after the first: draws the thick line.</summary>
    public bool IsNewAttempt { get; }

    public MessageHistoryRowKind Kind
    {
        get
        {
            if (Entry == null)
            {
                return Group.SummaryKind;
            }

            return Entry.Message.Event == "Failed" ? MessageHistoryRowKind.FailedMessage : MessageHistoryRowKind.Message;
        }
    }

    public string Title => Entry == null ? Group.SLTaskName : string.Create(CultureInfo.InvariantCulture, $"{Entry.Message.Timestamp:HH:mm:ss.fff}");

    public string AttemptText => Entry == null ? $"{Group.AttemptCount}" : $"{Entry.Attempt}";

    public string EventText => Entry == null ? "" : Entry.Message.Event;

    public string StatusText => Entry == null ? Group.FinalStatus : Entry.Message.Status;

    public string ProgressText => Entry == null ? "" : $"{Entry.Message.Progress:0}%";

    public string MessageText => Entry == null ? Group.Summary : Entry.Message.Message;

    [ObservableProperty] private string _Glyph = "";

    public void UpdateGlyph()
    {
        Glyph = Group.IsExpanded ? "▾" : "▸";
    }
}
