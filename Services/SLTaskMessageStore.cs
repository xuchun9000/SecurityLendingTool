using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Services;

/// <summary>
/// Saves every SLTask message to a SQLite database, %AppData%\SecurityLendingTool\SLTaskMessages.db,
/// table SLTaskMessage. Writes happen on a background thread, so the UI never waits for the disk.
/// <para>
/// Each run of a task (from Started or PreconditionNotMet to the end) gets its own RunId, so one
/// attempt can be queried on its own. Timestamps are stored as local time text "yyyy-MM-dd HH:mm:ss.fff",
/// which sorts and compares correctly in SQL.
/// </para>
/// </summary>
public sealed class SLTaskMessageStore : IDisposable
{
    private readonly BlockingCollection<SLTaskMessageRecord> _queue = new BlockingCollection<SLTaskMessageRecord>();
    private readonly ConcurrentDictionary<string, string> _currentRunIds = new ConcurrentDictionary<string, string>();
    private readonly Thread _writerThread;
    private readonly string _connectionString;
    private int _retentionMonths = 3;
    private volatile bool _purgeRequested = true;
    private DateTime _lastPurge = DateTime.MinValue;

    public SLTaskMessageStore()
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SecurityLendingTool");
        Directory.CreateDirectory(folder);
        DatabasePath = Path.Combine(folder, "SLTaskMessages.db");
        _connectionString = new SqliteConnectionStringBuilder { DataSource = DatabasePath }.ToString();
        CreateSchema();

        _writerThread = new Thread(WriteLoop) { IsBackground = true, Name = "SLTaskMessageStore writer" };
        _writerThread.Start();
    }

    /// <summary>Full path of the database file.</summary>
    public string DatabasePath { get; }

    /// <summary>
    /// Months of messages to keep (1 - 120). Older messages are deleted at startup, once a day,
    /// and right after this value changes.
    /// </summary>
    public int RetentionMonths
    {
        get => Volatile.Read(ref _retentionMonths);
        set
        {
            Volatile.Write(ref _retentionMonths, Math.Clamp(value, 1, 120));
            _purgeRequested = true;
        }
    }

    /// <summary>Starts saving every message the SLTask raises.</summary>
    public void Attach(ISLTask slTask)
    {
        slTask.ProgressChanged += SLTask_ProgressChanged;
    }

    private void SLTask_ProgressChanged(object? sender, SLTaskProgressEventArgs e)
    {
        if (sender is not ISLTask slTask || _queue.IsAddingCompleted)
        {
            return;
        }

        string runId;
        if (e.Event is SLTaskProgressEvent.Started or SLTaskProgressEvent.PreconditionNotMet)
        {
            runId = Guid.NewGuid().ToString("N");
            _currentRunIds[slTask.Name] = runId;
        }
        else
        {
            runId = _currentRunIds.GetOrAdd(slTask.Name, _ => Guid.NewGuid().ToString("N"));
        }

        _queue.Add(new SLTaskMessageRecord(DateTime.Now, slTask.Name, runId, e.Event, e.Status, e.Progress, e.Message));
    }

    private void CreateSchema()
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        // Incremental auto-vacuum lets the file shrink after old rows are deleted.
        // It only takes effect on an existing file after a one-time VACUUM.
        command.CommandText = "PRAGMA auto_vacuum;";
        long autoVacuum = (long)(command.ExecuteScalar() ?? 0L);
        if (autoVacuum != 2)
        {
            command.CommandText = "PRAGMA auto_vacuum = INCREMENTAL; VACUUM;";
            command.ExecuteNonQuery();
        }

        command.CommandText = """
            PRAGMA journal_mode = WAL;
            CREATE TABLE IF NOT EXISTS SLTaskMessage (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp   TEXT    NOT NULL,
                MachineName TEXT    NOT NULL,
                TaskName    TEXT    NOT NULL,
                RunId       TEXT    NOT NULL,
                Event       TEXT    NOT NULL,
                Status      TEXT    NOT NULL,
                Progress    REAL    NOT NULL,
                Message     TEXT    NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_SLTaskMessage_Timestamp ON SLTaskMessage (Timestamp);
            CREATE INDEX IF NOT EXISTS IX_SLTaskMessage_TaskName_Timestamp ON SLTaskMessage (TaskName, Timestamp);
            CREATE INDEX IF NOT EXISTS IX_SLTaskMessage_Event_Timestamp ON SLTaskMessage (Event, Timestamp);
            CREATE INDEX IF NOT EXISTS IX_SLTaskMessage_RunId ON SLTaskMessage (RunId);
            """;
        command.ExecuteNonQuery();
    }

    private void WriteLoop()
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();
        using SqliteCommand insert = connection.CreateCommand();
        insert.CommandText = """
            INSERT INTO SLTaskMessage (Timestamp, MachineName, TaskName, RunId, Event, Status, Progress, Message)
            VALUES ($timestamp, $machine, $task, $run, $event, $status, $progress, $message);
            """;
        SqliteParameter timestamp = insert.Parameters.Add("$timestamp", SqliteType.Text);
        SqliteParameter machine = insert.Parameters.Add("$machine", SqliteType.Text);
        SqliteParameter task = insert.Parameters.Add("$task", SqliteType.Text);
        SqliteParameter run = insert.Parameters.Add("$run", SqliteType.Text);
        SqliteParameter progressEvent = insert.Parameters.Add("$event", SqliteType.Text);
        SqliteParameter status = insert.Parameters.Add("$status", SqliteType.Text);
        SqliteParameter progress = insert.Parameters.Add("$progress", SqliteType.Real);
        SqliteParameter message = insert.Parameters.Add("$message", SqliteType.Text);
        machine.Value = Environment.MachineName;

        while (!_queue.IsCompleted)
        {
            if (_purgeRequested || DateTime.Now - _lastPurge >= TimeSpan.FromDays(1))
            {
                PurgeOldMessages(connection);
            }

            // Wake up at least once an hour, so the daily purge still runs when nothing is logged.
            if (!_queue.TryTake(out SLTaskMessageRecord? record, TimeSpan.FromHours(1)))
            {
                continue;
            }

            try
            {
                timestamp.Value = record.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                task.Value = record.TaskName;
                run.Value = record.RunId;
                progressEvent.Value = record.Event.ToString();
                status.Value = record.Status.ToString();
                progress.Value = record.Progress;
                message.Value = record.Message;
                insert.ExecuteNonQuery();
            }
            catch (SqliteException ex)
            {
                // Never let logging break the app; note it in the debug output.
                System.Diagnostics.Debug.WriteLine($"SLTaskMessageStore: could not save message for '{record.TaskName}': {ex.Message}");
            }
        }
    }

    /// <summary>Deletes messages older than <see cref="RetentionMonths"/> and gives the freed space back to the disk.</summary>
    private void PurgeOldMessages(SqliteConnection connection)
    {
        _purgeRequested = false;
        _lastPurge = DateTime.Now;
        try
        {
            string cutoff = DateTime.Now.AddMonths(-RetentionMonths).ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            using SqliteCommand delete = connection.CreateCommand();
            delete.CommandText = "DELETE FROM SLTaskMessage WHERE Timestamp < $cutoff;";
            delete.Parameters.AddWithValue("$cutoff", cutoff);
            int deleted = delete.ExecuteNonQuery();
            if (deleted > 0)
            {
                using SqliteCommand vacuum = connection.CreateCommand();
                vacuum.CommandText = "PRAGMA incremental_vacuum;";
                vacuum.ExecuteNonQuery();
            }

            System.Diagnostics.Debug.WriteLine($"SLTaskMessageStore: deleted {deleted} messages older than {cutoff}");
        }
        catch (SqliteException ex)
        {
            System.Diagnostics.Debug.WriteLine($"SLTaskMessageStore: purge failed: {ex.Message}");
        }
    }

    /// <summary>Writes whatever is still queued (up to 5 s), then stops the writer.</summary>
    public void Dispose()
    {
        if (_queue.IsAddingCompleted)
        {
            return;
        }

        _queue.CompleteAdding();
        _writerThread.Join(TimeSpan.FromSeconds(5));
    }

    private sealed record SLTaskMessageRecord(DateTime Timestamp, string TaskName, string RunId, SLTaskProgressEvent Event, SLTaskStatus Status, double Progress, string Message);
}
