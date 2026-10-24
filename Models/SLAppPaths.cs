using System.IO;

namespace SecurityLendingTool.Models;

/// <summary>App-wide folder names and paths, so the folder name is defined in one place.</summary>
public static class SLAppPaths
{
    /// <summary>Name of the app's folder under %AppData% and %LocalAppData%.</summary>
    public const string AppFolderName = "SecurityLendingTool";

    /// <summary>%AppData%\SecurityLendingTool (settings, message database).</summary>
    public static string AppDataFolder { get; } = 
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppFolderName);

    /// <summary>%LocalAppData%\SecurityLendingTool (machine-local output files).</summary>
    public static string LocalAppDataFolder { get; } = 
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppFolderName);
}
