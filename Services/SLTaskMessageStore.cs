using System.Collections.Concurrent;
using System.Threading.Channels;
using SecurityLendingTool.Models;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Services;

/// <summary>
/// Saves every SLTask message to a SQLite database, %AppData%\SecurityLendingTool\SLTaskMessages.db,
/// table SLTaskMessage. Writes happen on a background Task, so the UI never waits for the disk.
/// <para>
/// Each run of a task (from Started or PreconditionNotMet to the end) gets its own RunId, so one
/// attempt can be queried on its own. Timestamps are stored as local time text "yyyy-MM-dd HH:mm:ss.fff",
/// which sorts and compares correctly in SQL.
/// </para>
/// </summary>
public sealed class SLTaskMessageStore : IDisposable
{
    /// <summary>How often the writer wakes with nothing queued, so the daily purge still runs.</summary>
    private static readonly TimeSpan IdleWakeInterval = TimeSpan.FromHours(1);

    /// <summary>How often old messages are purged.</summary>
    private static readonly TimeSpan PurgeInterval = TimeSpan.FromDays(1);

    /// <summary>How long Dispose waits for queued messages to be written.</summary>
    private static readonly TimeSpan DisposeFlushTimeout = TimeSpan.FromSeconds(5);

    private readonly Channel<SLTaskMessageRecord> _queue = Channel.CreateUnbounded<SLTaskMessageRecord>(new UnboundedChannelOptions { SingleReader = true });
    private readonly ConcurrentDictionary<string, string> _currentRunIds = new ConcurrentDictionary<string, string>();
    private readonly Task _writerTask;
    private readonly string _connectionString;
    private int _retentionMonths = 3;
    private volatile bool _purgeRequested = true;
    private DateTime _lastPurge = DateTime.MinValue;

    public SLTaskMessageStore()
    {
        string folder = SLAppPaths.AppDataFolder;
        Directory.CreateDirectory(folder);
        DatabasePath = SLTaskMessageDb.DatabasePath;
        _connectionString = new SqliteConnectionStringBuilder { DataSource = DatabasePath }.ToString();
        CreateSchema();

        _writerTask = Task.Run(WriteLoopAsync);
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
        if (sender is not ISLTask slTask)
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

        // TryWrite returns false once the store is disposed; the message is then dropped.
        _queue.Writer.TryWrite(new SLTaskMessageRecord(DateTime.Now, slTask.Name, runId, e.Event, e.Status, e.Progress, e.Message));
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

        command.CommandText = $"""
            PRAGMA journal_mode = WAL;
            CREATE TABLE IF NOT EXISTS {SLTaskMessageDb.TableName} (
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
            CREATE INDEX IF NOT EXISTS IX_SLTaskMessage_Timestamp ON {SLTaskMessageDb.TableName} (Timestamp);
            CREATE INDEX IF NOT EXISTS IX_SLTaskMessage_TaskName_Timestamp ON {SLTaskMessageDb.TableName} (TaskName, Timestamp);
            CREATE INDEX IF NOT EXISTS IX_SLTaskMessage_Event_Timestamp ON {SLTaskMessageDb.TableName} (Event, Timestamp);
            CREATE INDEX IF NOT EXISTS IX_SLTaskMessage_RunId ON {SLTaskMessageDb.TableName} (RunId);
            """;
        command.ExecuteNonQuery();
    }

    private async Task WriteLoopAsync()
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();
        using SqliteCommand insert = connection.CreateCommand();
        insert.CommandText = $"""
            INSERT INTO {SLTaskMessageDb.TableName} (Timestamp, MachineName, TaskName, RunId, Event, Status, Progress, Message)
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

        ChannelReader<SLTaskMessageRecord> reader = _queue.Reader;
        while (true)
        {
            if (_purgeRequested || DateTime.Now - _lastPurge >= PurgeInterval)
            {
                PurgeOldMessages(connection);
            }

            bool hasMore;
            using (CancellationTokenSource idle = new CancellationTokenSource(IdleWakeInterval))
            {
                try
                {
                    hasMore = await reader.WaitToReadAsync(idle.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Nothing logged for a while; loop round so the daily purge can run.
                    continue;
                }
            }

            if (!hasMore)
            {
                // Completed by Dispose and fully drained.
                return;
            }

            while (reader.TryRead(out SLTaskMessageRecord? record))
            {
                try
                {
                    timestamp.Value = record.Timestamp.ToString(SLTaskMessageDb.TimestampFormat, CultureInfo.InvariantCulture);
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
    }
    /// <summary>Deletes messages older than <see cref="RetentionMonths"/> and gives the freed space back to the disk.</summary>
    private void PurgeOldMessages(SqliteConnection connection)
    {
        _purgeRequested = false;
        _lastPurge = DateTime.Now;
        try
        {
            string cutoff = DateTime.Now.AddMonths(-RetentionMonths).ToString(SLTaskMessageDb.TimestampFormat, CultureInfo.InvariantCulture);
            using SqliteCommand delete = connection.CreateCommand();
            delete.CommandText = $"DELETE FROM {SLTaskMessageDb.TableName} WHERE Timestamp < $cutoff;";
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

    /// <summary>Writes whatever is still queued (up to <see cref="DisposeFlushTimeout"/>), then stops the writer Task.</summary>
    public void Dispose()
    {
        if (!_queue.Writer.TryComplete())
        {
            return;
        }

        try
        {
            _writerTask.Wait(DisposeFlushTimeout);
        }
        catch (AggregateException ex)
        {
            System.Diagnostics.Debug.WriteLine($"SLTaskMessageStore: writer failed: {ex.InnerException?.Message}");
        }
    }

    private sealed record SLTaskMessageRecord(DateTime Timestamp, string TaskName, string RunId, SLTaskProgressEvent Event, SLTaskStatus Status, double Progress, string Message);
}
