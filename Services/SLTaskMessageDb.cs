using System.IO;
using SecurityLendingTool.Models;

namespace SecurityLendingTool.Services;

/// <summary>Names and formats shared by the message database writer (<see cref="SLTaskMessageStore"/>) and reader (<see cref="SLTaskMessageReader"/>).</summary>
public static class SLTaskMessageDb
{
    /// <summary>File name of the SQLite database under %AppData%\SecurityLendingTool.</summary>
    public const string FileName = "SLTaskMessages.db";

    /// <summary>Table that holds one row per SLTask message.</summary>
    public const string TableName = "SLTaskMessage";

    /// <summary>How Timestamp is stored (local time); this text sorts and compares correctly in SQL.</summary>
    public const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";

    /// <summary>Full path of the database file.</summary>
    public static string DatabasePath { get; } = Path.Combine(SLAppPaths.AppDataFolder, FileName);
}
