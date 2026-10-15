using global::Weather.Modules.Weather.Domain;
using global::Weather.Modules.Weather.Domain.Forecasts;
using global::Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.Modules.Weather.Tests.UnitTests.Domain;

[Trait("Category", "ForecastDomain")]
public sealed class ForecastDomainTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(50, 50)]
    [InlineData(100, 100)]
    public void FromPercent_KeepsValue(int percent, int expected)
    {
        Assert.Equal(expected, PrecipitationProbability.FromPercent(percent).Percent);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void FromPercent_RejectsOutOfRange(int percent)
    {
        Assert.Throws<WeatherDomainException>(() => PrecipitationProbability.FromPercent(percent));
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.5, 50)]
    [InlineData(0.75, 75)]
    [InlineData(1.0, 100)]
    public void FromRatio_ConvertsToPercent(double ratio, int expected)
    {
        Assert.Equal(expected, PrecipitationProbability.FromRatio(ratio).Percent);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void FromRatio_RejectsOutOfRange(double ratio)
    {
        Assert.Throws<WeatherDomainException>(() => PrecipitationProbability.FromRatio(ratio));
    }

    [Fact]
    public void ForecastInterval_AcceptsUtc()
    {
        ForecastInterval interval = ForecastInterval.Create(
            DateTimeOffset.UnixEpoch,
            Temperature.FromCelsius(10),
            PrecipitationProbability.FromPercent(20),
            SkyCondition.Cloudy);

        Assert.Equal(10m, interval.Temperature.Celsius);
        Assert.Equal(20, interval.Precipitation.Percent);
    }

    [Fact]
    public void ForecastInterval_RejectsNonUtc()
    {
        Assert.Throws<WeatherDomainException>(() => ForecastInterval.Create(
            new DateTimeOffset(2025, 10, 13, 12, 0, 0, TimeSpan.FromHours(2)),
            Temperature.FromCelsius(10),
            PrecipitationProbability.FromPercent(20),
            SkyCondition.Cloudy));
    }

    [Fact]
    public void DailyForecast_RejectsLowAboveHigh()
    {
        Assert.Throws<WeatherDomainException>(() => DailyForecast.Create(
            new DateOnly(2025, 10, 13),
            Temperature.FromCelsius(8),
            Temperature.FromCelsius(12),
            PrecipitationProbability.FromPercent(20),
            SkyCondition.Cloudy));
    }
}
