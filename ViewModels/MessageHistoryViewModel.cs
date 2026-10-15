using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using SecurityLendingTool.Services;

namespace SecurityLendingTool.ViewModels;

/// <summary>
/// The database Message History window: a Year / Month / Day tree on the left and the selected
/// day's messages, grouped by SLTask, on the right.
/// </summary>
public partial class MessageHistoryViewModel : ObservableObject
{
    public const string AllSLTasksOption = "(All)";

    private readonly string _databasePath;
    private List<MessageHistorySLTaskGroup> _groups = new List<MessageHistorySLTaskGroup>();
    private DateOnly? _loadedDate;
    private int _loadVersion;

    public MessageHistoryViewModel(string databasePath, int retentionMonths)
    {
        _databasePath = databasePath;
        FooterText = $"Data: {databasePath} (keeps {retentionMonths} months)";
    }

    /// <summary>Set by the view: asks where to save the CSV file; null on Cancel.</summary>
    public Func<string, string?>? AskExportPath { get; set; }

    public string FooterText { get; }

    public ObservableCollection<MessageHistoryTreeNode> Years { get; } = new ObservableCollection<MessageHistoryTreeNode>();

    public ObservableCollection<MessageHistoryRow> Rows { get; } = new ObservableCollection<MessageHistoryRow>();

    public ObservableCollection<string> SLTaskFilterOptions { get; } = new ObservableCollection<string> { AllSLTasksOption };

    [ObservableProperty] private MessageHistoryTreeNode? _SelectedDay;

    [ObservableProperty] private string? _SelectedSLTaskFilter = AllSLTasksOption;

    [ObservableProperty] private bool _IsFailedOnly;

    [ObservableProperty] private bool _IsProgressHidden = true;

    [ObservableProperty] private string _SearchText = "";

    [ObservableProperty] private string _DayTitle = "";

    [ObservableProperty] private string _SLTaskCountText = "";

    [ObservableProperty] private string _CompletedCountText = "";

    [ObservableProperty] private string _FailedCountText = "";

    [ObservableProperty] private string _PassCountText = "";

    [ObservableProperty] private string _TimeRangeText = "";

    [ObservableProperty] private string _InfoText = "";

    /// <summary>Reloads the day list from the database and keeps (or picks) the selected day.</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        DateOnly? keepDate = SelectedDay?.Date;
        IReadOnlyList<SLTaskHistoryDay> days;
        try
        {
            days = await Task.Run(() => SLTaskMessageReader.ReadDays(_databasePath));
        }
        catch (SqliteException ex)
        {
            InfoText = $"Could not read the message database: {ex.Message}";
            return;
        }

        Years.Clear();
        MessageHistoryTreeNode? yearNode = null;
        MessageHistoryTreeNode? monthNode = null;
        List<MessageHistoryTreeNode> dayNodes = new List<MessageHistoryTreeNode>();
        foreach (SLTaskHistoryDay day in days)
        {
            if (yearNode == null || yearNode.Date.Year != day.Date.Year)
            {
                yearNode = new MessageHistoryTreeNode(MessageHistoryNodeKind.Year, string.Create(CultureInfo.InvariantCulture, $"{day.Date:yyyy}"), day.Date, 0);
                Years.Add(yearNode);
                monthNode = null;
            }

            if (monthNode == null || monthNode.Date.Month != day.Date.Month)
            {
                monthNode = new MessageHistoryTreeNode(MessageHistoryNodeKind.Month, string.Create(CultureInfo.InvariantCulture, $"{day.Date:MM MMMM}"), day.Date, 0);
                yearNode.Children.Add(monthNode);
            }

            MessageHistoryTreeNode dayNode = new MessageHistoryTreeNode(MessageHistoryNodeKind.Day, string.Create(CultureInfo.InvariantCulture, $"{day.Date:ddd dd}"), day.Date, day.NotCompletedCount);
            monthNode.Children.Add(dayNode);
            dayNodes.Add(dayNode);
        }

        if (dayNodes.Count == 0)
        {
            InfoText = File.Exists(_databasePath) ? "No messages saved yet." : "The message database does not exist yet; run some SLTasks first.";
            SelectedDay = null;
            ClearDay();
            return;
        }

        InfoText = "";
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        MessageHistoryTreeNode select = dayNodes.FirstOrDefault(node => node.Date == keepDate)
            ?? dayNodes.FirstOrDefault(node => node.Date == today)
            ?? dayNodes[0];
        foreach (MessageHistoryTreeNode year in Years)
        {
            foreach (MessageHistoryTreeNode month in year.Children)
            {
                if (month.Children.Contains(select))
                {
                    year.IsExpanded = true;
                    month.IsExpanded = true;
                }
            }
        }

        select.IsSelected = true;
        _loadedDate = null;
        if (SelectedDay == select)
        {
            await LoadDayAsync(select.Date);
        }
        else
        {
            SelectedDay = select;
        }
    }

    partial void OnSelectedDayChanged(MessageHistoryTreeNode? value)
    {
        if (value != null && value.Kind == MessageHistoryNodeKind.Day && value.Date != _loadedDate)
        {
            _ = LoadDayAsync(value.Date);
        }
    }

    partial void OnSelectedSLTaskFilterChanged(string? value)
    {
        RebuildRows();
    }

    partial void OnIsFailedOnlyChanged(bool value)
    {
        RebuildRows();
    }

    partial void OnIsProgressHiddenChanged(bool value)
    {
        RebuildRows();
    }

    partial void OnSearchTextChanged(string value)
    {
        RebuildRows();
    }

    private async Task LoadDayAsync(DateOnly date)
    {
        int version = ++_loadVersion;
        _loadedDate = date;
        IReadOnlyList<SLTaskStoredMessage> messages;
        try
        {
            messages = await Task.Run(() => SLTaskMessageReader.ReadDay(_databasePath, date));
        }
        catch (SqliteException ex)
        {
            InfoText = $"Could not read the message database: {ex.Message}";
            return;
        }

        if (version != _loadVersion)
        {
            // A newer day was selected while this one was loading.
            return;
        }

        _groups = BuildGroups(messages);
        string previousFilter = SelectedSLTaskFilter ?? AllSLTasksOption;
        SLTaskFilterOptions.Clear();
        SLTaskFilterOptions.Add(AllSLTasksOption);
        foreach (MessageHistorySLTaskGroup group in _groups)
        {
            SLTaskFilterOptions.Add(group.SLTaskName);
        }

        SelectedSLTaskFilter = SLTaskFilterOptions.Contains(previousFilter) ? previousFilter : AllSLTasksOption;

        int completed = _groups.Count(group => group.FinalStatus == "Completed");
        int passes = _groups.Count == 0 ? 0 : _groups.Max(group => group.AttemptCount);
        DayTitle = string.Create(CultureInfo.InvariantCulture, $"{date:dddd, yyyy-MM-dd}");
        SLTaskCountText = $"{_groups.Count} tasks";
        CompletedCountText = $"{completed} Completed";
        FailedCountText = $"{_groups.Count - completed} Failed";
        PassCountText = passes == 1 ? "1 pass" : $"{passes} passes";
        TimeRangeText = messages.Count == 0
            ? ""
            : string.Create(CultureInfo.InvariantCulture, $"{messages[0].Timestamp:HH:mm:ss} – {messages[^1].Timestamp:HH:mm:ss}");
        RebuildRows();
    }

    private static List<MessageHistorySLTaskGroup> BuildGroups(IReadOnlyList<SLTaskStoredMessage> messages)
    {
        // Messages are in time order, so the groups come out ordered by each SLTask's first message.
        Dictionary<string, List<SLTaskStoredMessage>> bySLTask = new Dictionary<string, List<SLTaskStoredMessage>>();
        List<string> order = new List<string>();
        foreach (SLTaskStoredMessage message in messages)
        {
            if (!bySLTask.TryGetValue(message.SLTaskName, out List<SLTaskStoredMessage>? list))
            {
                list = new List<SLTaskStoredMessage>();
                bySLTask.Add(message.SLTaskName, list);
                order.Add(message.SLTaskName);
            }

            list.Add(message);
        }

        List<MessageHistorySLTaskGroup> groups = new List<MessageHistorySLTaskGroup>();
        foreach (string slTaskName in order)
        {
            List<MessageHistoryEntry> entries = new List<MessageHistoryEntry>();
            int attempt = 0;
            string? runId = null;
            foreach (SLTaskStoredMessage message in bySLTask[slTaskName])
            {
                // Every attempt (from Started or PreconditionNotMet) has its own RunId.
                if (attempt == 0 || message.RunId != runId)
                {
                    attempt++;
                    runId = message.RunId;
                }

                entries.Add(new MessageHistoryEntry(message, attempt));
            }

            groups.Add(new MessageHistorySLTaskGroup(slTaskName, entries));
        }

        return groups;
    }

    private void ClearDay()
    {
        _loadedDate = null;
        _groups = new List<MessageHistorySLTaskGroup>();
        DayTitle = "";
        SLTaskCountText = "";
        CompletedCountText = "";
        FailedCountText = "";
        PassCountText = "";
        TimeRangeText = "";
        RebuildRows();
    }

    private bool IsGroupShown(MessageHistorySLTaskGroup group)
    {
        string filter = SelectedSLTaskFilter ?? AllSLTasksOption;
        if (filter != AllSLTasksOption && filter != group.SLTaskName)
        {
            return false;
        }

        if (IsFailedOnly && !group.HasFailure && group.FinalStatus == "Completed")
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText) || group.SLTaskName.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return GetShownEntries(group).Count > 0;
    }

    /// <summary>The group's messages that pass the Progress and Search filters.</summary>
    private List<MessageHistoryEntry> GetShownEntries(MessageHistorySLTaskGroup group)
    {
        string search = SearchText.Trim();
        bool nameMatches = search.Length > 0 && group.SLTaskName.Contains(search, StringComparison.OrdinalIgnoreCase);
        List<MessageHistoryEntry> shown = new List<MessageHistoryEntry>();
        foreach (MessageHistoryEntry entry in group.Entries)
        {
            if (IsProgressHidden && entry.Message.Event == "Progress")
            {
                continue;
            }

            if (search.Length > 0 && !nameMatches && !entry.Message.Message.Contains(search, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            shown.Add(entry);
        }

        return shown;
    }

    private List<MessageHistoryRow> BuildDetailRows(MessageHistorySLTaskGroup group)
    {
        List<MessageHistoryRow> rows = new List<MessageHistoryRow>();
        int previousAttempt = 0;
        foreach (MessageHistoryEntry entry in GetShownEntries(group))
        {
            bool isNewAttempt = previousAttempt != 0 && entry.Attempt != previousAttempt;
            rows.Add(MessageHistoryRow.ForEntry(group, entry, isNewAttempt));
            previousAttempt = entry.Attempt;
        }

        return rows;
    }

    private void RebuildRows()
    {
        Rows.Clear();
        foreach (MessageHistorySLTaskGroup group in _groups)
        {
            if (!IsGroupShown(group))
            {
                continue;
            }

            Rows.Add(MessageHistoryRow.ForSummary(group));
            if (group.IsExpanded)
            {
                foreach (MessageHistoryRow row in BuildDetailRows(group))
                {
                    Rows.Add(row);
                }
            }
        }
    }

    /// <summary>Expands or collapses one SLTask's messages (▸ / ▾).</summary>
    [RelayCommand]
    private void ToggleGroup(MessageHistoryRow? row)
    {
        if (row == null || !row.IsSummary)
        {
            return;
        }

        int index = Rows.IndexOf(row);
        if (index < 0)
        {
            return;
        }

        row.Group.IsExpanded = !row.Group.IsExpanded;
        row.UpdateGlyph();
        if (row.Group.IsExpanded)
        {
            int insertAt = index + 1;
            foreach (MessageHistoryRow detail in BuildDetailRows(row.Group))
            {
                Rows.Insert(insertAt, detail);
                insertAt++;
            }
        }
        else
        {
            while (index + 1 < Rows.Count && !Rows[index + 1].IsSummary)
            {
                Rows.RemoveAt(index + 1);
            }
        }
    }

    /// <summary>Saves the selected day's messages (with the current filters) to a CSV file.</summary>
    [RelayCommand]
    private void ExportCsv()
    {
        if (_loadedDate == null || AskExportPath == null)
        {
            return;
        }

        string suggestedName = string.Create(CultureInfo.InvariantCulture, $"SLTaskMessages-{_loadedDate.Value:yyyyMMdd}.csv");
        string? path = AskExportPath(suggestedName);
        if (path == null)
        {
            return;
        }

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Timestamp,Task,Attempt,RunId,Event,Status,Progress,Message");
        foreach (MessageHistorySLTaskGroup group in _groups)
        {
            if (!IsGroupShown(group))
            {
                continue;
            }

            foreach (MessageHistoryEntry entry in GetShownEntries(group))
            {
                SLTaskStoredMessage message = entry.Message;
                csv.AppendLine(string.Create(CultureInfo.InvariantCulture,
                    $"{message.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{EscapeCsv(message.SLTaskName)},{entry.Attempt},{message.RunId},{EscapeCsv(message.Event)},{EscapeCsv(message.Status)},{message.Progress:0.##},{EscapeCsv(message.Message)}"));
            }
        }

        try
        {
            // UTF-8 with BOM so Excel shows non-ASCII text correctly.
            File.WriteAllText(path, csv.ToString(), new UTF8Encoding(true));
            InfoText = $"Exported to {path}";
        }
        catch (IOException ex)
        {
            InfoText = $"Export failed: {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            InfoText = $"Export failed: {ex.Message}";
        }
    }

    private static string EscapeCsv(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
        {
            return value;
        }

        string escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
