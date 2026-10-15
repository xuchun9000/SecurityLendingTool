namespace SecurityLendingTool.Tasks.Implementations;

/// <summary>The result of <see cref="DemoTask"/>, stored in its Result property.</summary>
public sealed record WeatherResult(
    string City,
    double TemperatureC,
    double WindSpeedKmh,
    int WeatherCode,
    string WeatherDescription,
    DateTime? ObservedAt,
    string RawJson,
    string ResultFilePath);