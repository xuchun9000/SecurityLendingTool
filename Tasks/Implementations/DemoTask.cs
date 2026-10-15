using System.IO;
using System.Net.Http;
using System.Text.Json;
using SecurityLendingTool.Tasks.Core;

namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>
/// Demo task: reads the current weather from Open-Meteo (free, no API key),
/// saves the raw JSON to a file, and exposes the result through properties.
/// Run it with <see cref="SLTaskBase.Start"/>.
/// </summary>
public sealed class DemoTask : SLTaskBase
{
    private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

    private double? temperatureC;
    private double? windSpeedKmh;
    private int? weatherCode;
    private string weatherDescription = string.Empty;
    private DateTime? observedAt;
    private string rawJson = string.Empty;
    private string? resultFilePath;

    public DemoTask(string city = "New York", double latitude = 40.7128, double longitude = -74.0060)
        : base("Demo Weather", $"Read current weather for {city} and save it to a file.")
    {
        City = city;
        Latitude = latitude;
        Longitude = longitude;
    }

    // ---------- Input ----------

    public string City { get; }

    public double Latitude { get; }

    public double Longitude { get; }

    // ---------- Result ----------

    /// <summary>Temperature in °C.</summary>
    public double? TemperatureC
    {
        get => temperatureC;
        private set => SetProperty(ref temperatureC, value);
    }

    /// <summary>Wind speed in km/h.</summary>
    public double? WindSpeedKmh
    {
        get => windSpeedKmh;
        private set => SetProperty(ref windSpeedKmh, value);
    }

    /// <summary>WMO weather code from Open-Meteo.</summary>
    public int? WeatherCode
    {
        get => weatherCode;
        private set => SetProperty(ref weatherCode, value);
    }

    /// <summary>Readable text for <see cref="WeatherCode"/>, e.g. "Clear sky".</summary>
    public string WeatherDescription
    {
        get => weatherDescription;
        private set => SetProperty(ref weatherDescription, value);
    }

    /// <summary>Observation time (local time of the location).</summary>
    public DateTime? ObservedAt
    {
        get => observedAt;
        private set => SetProperty(ref observedAt, value);
    }

    /// <summary>The full JSON returned by the server.</summary>
    public string RawJson
    {
        get => rawJson;
        private set => SetProperty(ref rawJson, value);
    }

    /// <summary>Where the result was saved.</summary>
    public string? ResultFilePath
    {
        get => resultFilePath;
        private set => SetProperty(ref resultFilePath, value);
    }

    /// <summary>Typed view of <see cref="SLTaskBase.Result"/>.</summary>
    public WeatherResult? Weather => Result as WeatherResult;

    // ---------- Work ----------

    protected override async Task<object?> ExecuteAsync(CancellationToken cancellationToken)
    {
        ClearResult();

        string url = string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"https://api.open-meteo.com/v1/forecast?latitude={Latitude}&longitude={Longitude}&current_weather=true");

        ReportProgress(10, $"Downloading weather for {City}...");
        string json = await Http.GetStringAsync(url, cancellationToken);
        ReportProgress(60, "Parsing weather data...");

        using (JsonDocument doc = JsonDocument.Parse(json))
        {
            JsonElement current = doc.RootElement.GetProperty("current_weather");
            TemperatureC = current.GetProperty("temperature").GetDouble();
            WindSpeedKmh = current.GetProperty("windspeed").GetDouble();
            WeatherCode = current.GetProperty("weathercode").GetInt32();
            WeatherDescription = DescribeWeatherCode(WeatherCode.Value);

            DateTime time;
            if (DateTime.TryParse(current.GetProperty("time").GetString(), out time))
            {
                ObservedAt = time;
            }
        }

        RawJson = json;
        ReportProgress(80, "Saving result to file...");

        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SecurityLendingTool",
            "Weather");
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, $"weather_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        await File.WriteAllTextAsync(file, json, cancellationToken);
        ResultFilePath = file;
        ReportProgress(95, $"Saved to {file}");

        return new WeatherResult(
            City,
            TemperatureC.Value,
            WindSpeedKmh.Value,
            WeatherCode.Value,
            WeatherDescription,
            ObservedAt,
            RawJson,
            file);
    }
    /// <summary>Needs a network connection to reach Open-Meteo.</summary>
    public override SLPreConditionResult CheckPreCondition()
    {
        if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
        {
            return SLPreConditionResult.NotMet("No network connection");
        }

        return SLPreConditionResult.Met;
    }

    public override void OnFinished()
    {
    }

    protected override void OnStop()
    {
        ReportProgress(Progress, "Stop requested, canceling download...");
    }

    protected override void OnReset()
    {
        ClearResult();
    }

    private void ClearResult()
    {
        TemperatureC = null;
        WindSpeedKmh = null;
        WeatherCode = null;
        WeatherDescription = string.Empty;
        ObservedAt = null;
        RawJson = string.Empty;
        ResultFilePath = null;
    }

    private static string DescribeWeatherCode(int code)
    {
        switch (code)
        {
            case 0: return "Clear sky";
            case 1: return "Mainly clear";
            case 2: return "Partly cloudy";
            case 3: return "Overcast";
            case 45:
            case 48: return "Fog";
            case 51:
            case 53:
            case 55: return "Drizzle";
            case 61:
            case 63:
            case 65: return "Rain";
            case 71:
            case 73:
            case 75: return "Snow";
            case 80:
            case 81:
            case 82: return "Rain showers";
            case 95:
            case 96:
            case 99: return "Thunderstorm";
            default: return $"Code {code}";
        }
    }
}