using System.IO;
using System.Text.Json;

namespace SecurityLendingTool.Models;

/// <summary>Daily start/stop time for all SLTasks, saved as JSON in %AppData%\SecurityLendingTool\schedule.json.</summary>
public sealed class SLScheduleSettings
{
    public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);

    public TimeSpan StopTime { get; set; } = new TimeSpan(17, 0, 0);

    /// <summary>Seconds to wait before the next pass retries SLTasks that did not complete (minimum 5).</summary>
    public int RetryIntervalSeconds { get; set; } = 60;

    /// <summary>Months of SLTask messages kept in the database; older ones are deleted automatically (1 - 120).</summary>
    public int MessageRetentionMonths { get; set; } = 3;

    public static string FilePath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SecurityLendingTool", "schedule.json");

    public static SLScheduleSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                SLScheduleSettings? settings = JsonSerializer.Deserialize<SLScheduleSettings>(json);
                if (settings != null)
                {
                    if (settings.RetryIntervalSeconds < 5)
                    {
                        settings.RetryIntervalSeconds = 5;
                    }

                    settings.MessageRetentionMonths = Math.Clamp(settings.MessageRetentionMonths, 1, 120);

                    return settings;
                }
            }
        }
        catch (Exception)
        {
            // Bad file: fall back to defaults.
        }

        return new SLScheduleSettings();
    }

    public void Save()
    {
        string? folder = Path.GetDirectoryName(FilePath);
        if (folder != null)
        {
            Directory.CreateDirectory(folder);
        }

        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}
