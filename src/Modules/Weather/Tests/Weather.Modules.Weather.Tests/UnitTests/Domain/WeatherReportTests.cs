using global::Weather.Modules.Weather.Domain;
using global::Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.Modules.Weather.Tests.UnitTests.Domain;

[Trait("Category", "WeatherReport")]
public sealed class WeatherReportTests
{
    private static WeatherReport CreateReport(DateTimeOffset observedAtUtc) => WeatherReport.Create(
        Location.Of("London", "GB"),
        observedAtUtc,
        Temperature.FromFahrenheit(50m),
        RelativeHumidity.FromPercent(80),
        WindInfo.FromMetersPerSecond(4m, 220),
        Visibility.FromMeters(10000m),
        Pressure.FromHectopascals(1013m),
        SkyCondition.Cloudy);

    [Fact]
    public void Create_DerivesDewPoint()
    {
        WeatherReport report = CreateReport(new DateTimeOffset(2025, 10, 13, 12, 0, 0, TimeSpan.Zero));

        Assert.True(report.DewPoint.Celsius <= report.Temperature.Celsius);
    }

    [Fact]
    public void Create_RejectsNonUtcTimestamp()
    {
        DateTimeOffset localWithOffset = new(2025, 10, 13, 12, 0, 0, TimeSpan.FromHours(2));

        Assert.Throws<WeatherDomainException>(() => CreateReport(localWithOffset));
    }
}
