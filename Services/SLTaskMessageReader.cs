using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;

namespace SecurityLendingTool.Services;

/// <summary>One message read back from the SLTaskMessage table.</summary>
public sealed record SLTaskStoredMessage(DateTime Timestamp, string SLTaskName, string RunId, string Event, string Status, double Progress, string Message);

/// <summary>A day that has messages, and how many SLTasks did not end that day as Completed.</summary>
public sealed record SLTaskHistoryDay(DateOnly Date, int SLTaskCount, int NotCompletedCount);

/// <summary>
/// Reads the message database written by <see cref="SLTaskMessageStore"/>, using a read-only connection.
/// A missing database file gives empty results.
/// </summary>
public static class SLTaskMessageReader
{
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";

    /// <summary>All days with messages, newest first.</summary>
    public static IReadOnlyList<SLTaskHistoryDay> ReadDays(string databasePath)
    {
        List<SLTaskHistoryDay> days = new List<SLTaskHistoryDay>();
        if (!File.Exists(databasePath))
        {
            return days;
        }

        using SqliteConnection connection = Open(databasePath);
        if (!TableExists(connection))
        {
            return days;
        }

        using SqliteCommand command = connection.CreateCommand();
        // With a single MAX() aggregate, SQLite takes the bare Status column from the row with the
        // highest Id, i.e. the last message of that SLTask on that day (its final status).
        command.CommandText = """
            SELECT substr(Timestamp, 1, 10) AS Day, TaskName, Status, MAX(Id)
            FROM SLTaskMessage
            GROUP BY Day, TaskName
            ORDER BY Day DESC;
            """;
        using SqliteDataReader reader = command.ExecuteReader();
        string? currentDay = null;
        int slTaskCount = 0;
        int notCompletedCount = 0;
        while (reader.Read())
        {
            string day = reader.GetString(0);
            if (day != currentDay)
            {
                AddDay(days, currentDay, slTaskCount, notCompletedCount);
                currentDay = day;
                slTaskCount = 0;
                notCompletedCount = 0;
            }

            slTaskCount++;
            if (reader.GetString(2) != "Completed")
            {
                notCompletedCount++;
            }
        }

        AddDay(days, currentDay, slTaskCount, notCompletedCount);
        return days;
    }

    /// <summary>All messages of one day, in time order.</summary>
    public static IReadOnlyList<SLTaskStoredMessage> ReadDay(string databasePath, DateOnly date)
    {
        List<SLTaskStoredMessage> messages = new List<SLTaskStoredMessage>();
        if (!File.Exists(databasePath))
        {
            return messages;
        }

        using SqliteConnection connection = Open(databasePath);
        if (!TableExists(connection))
        {
            return messages;
        }

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT Timestamp, TaskName, RunId, Event, Status, Progress, Message
            FROM SLTaskMessage
            WHERE Timestamp >= $from AND Timestamp < $to
            ORDER BY Timestamp, Id;
            """;
        command.Parameters.AddWithValue("$from", date.ToDateTime(TimeOnly.MinValue).ToString(TimestampFormat, CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$to", date.AddDays(1).ToDateTime(TimeOnly.MinValue).ToString(TimestampFormat, CultureInfo.InvariantCulture));
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            DateTime timestamp = DateTime.ParseExact(reader.GetString(0), TimestampFormat, CultureInfo.InvariantCulture);
            messages.Add(new SLTaskStoredMessage(timestamp, reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetDouble(5), reader.GetString(6)));
        }

        return messages;
    }

    private static void AddDay(List<SLTaskHistoryDay> days, string? day, int slTaskCount, int notCompletedCount)
    {
        if (day == null)
        {
            return;
        }

        if (DateOnly.TryParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
        {
            days.Add(new SLTaskHistoryDay(date, slTaskCount, notCompletedCount));
        }
    }

    private static SqliteConnection Open(string databasePath)
    {
        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        }.ToString();
        SqliteConnection connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private static bool TableExists(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'SLTaskMessage';";
        long count = (long)(command.ExecuteScalar() ?? 0L);
        return count > 0;
    }
}
