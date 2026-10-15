using global::Weather.Modules.Weather.Domain;
using global::Weather.Modules.Weather.Domain.Forecasts;
using global::Weather.Modules.Weather.Domain.WeatherReports;

namespace Weather.Modules.Weather.Tests.UnitTests.Domain;

[Trait("Category", "ForecastAggregation")]
public sealed class ForecastAggregationTests
{
    private static readonly Location London = Location.Of("London", "GB");

    private static ForecastInterval Interval(
        DateTimeOffset atUtc,
        decimal celsius,
        int precipPercent = 0,
        SkyCondition? sky = null) => ForecastInterval.Create(
            atUtc,
            Temperature.FromCelsius(celsius),
            PrecipitationProbability.FromPercent(precipPercent),
            sky ?? SkyCondition.Cloudy);

    [Fact]
    public void SameLocalDay_GroupsIntoOneDay_WithHighAndLow()
    {
        ForecastInterval[] intervals =
        [
            Interval(new DateTimeOffset(2025, 10, 13, 6, 0, 0, TimeSpan.Zero), 8m),
            Interval(new DateTimeOffset(2025, 10, 13, 15, 0, 0, TimeSpan.Zero), 14m),
        ];

        WeatherForecast forecast = WeatherForecast.FromIntervals(London, intervals, TimeSpan.Zero);

        DailyForecast day = Assert.Single(forecast.Days);
        Assert.Equal(new DateOnly(2025, 10, 13), day.Date);
        Assert.Equal(14m, day.High.Celsius);
        Assert.Equal(8m, day.Low.Celsius);
    }

    [Fact]
    public void LocalOffset_KeepsTwoUtcDaysOnTheSameLocalDay()
    {
        // 23:30Z and 00:30Z are different UTC days but the same day at UTC+02:00.
        ForecastInterval[] intervals =
        [
            Interval(new DateTimeOffset(2025, 10, 13, 23, 30, 0, TimeSpan.Zero), 6m),
            Interval(new DateTimeOffset(2025, 10, 14, 0, 30, 0, TimeSpan.Zero), 9m),
        ];

        WeatherForecast forecast = WeatherForecast.FromIntervals(London, intervals, TimeSpan.FromHours(2));

        DailyForecast day = Assert.Single(forecast.Days);
        Assert.Equal(new DateOnly(2025, 10, 14), day.Date);
    }

    [Fact]
    public void DaysAreOrderedAscending()
    {
        ForecastInterval[] intervals =
        [
            Interval(new DateTimeOffset(2025, 10, 15, 9, 0, 0, TimeSpan.Zero), 10m),
            Interval(new DateTimeOffset(2025, 10, 13, 9, 0, 0, TimeSpan.Zero), 10m),
            Interval(new DateTimeOffset(2025, 10, 14, 9, 0, 0, TimeSpan.Zero), 10m),
        ];

        WeatherForecast forecast = WeatherForecast.FromIntervals(London, intervals, TimeSpan.Zero);

        Assert.Equal(
            [new DateOnly(2025, 10, 13), new DateOnly(2025, 10, 14), new DateOnly(2025, 10, 15)],
            forecast.Days.Select(day => day.Date).ToArray());
    }

    [Fact]
    public void PrecipitationProbability_IsTheDailyMaximum()
    {
        ForecastInterval[] intervals =
        [
            Interval(new DateTimeOffset(2025, 10, 13, 6, 0, 0, TimeSpan.Zero), 8m, 20),
            Interval(new DateTimeOffset(2025, 10, 13, 9, 0, 0, TimeSpan.Zero), 9m, 75),
            Interval(new DateTimeOffset(2025, 10, 13, 12, 0, 0, TimeSpan.Zero), 10m, 40),
        ];

        WeatherForecast forecast = WeatherForecast.FromIntervals(London, intervals, TimeSpan.Zero);

        Assert.Equal(75, Assert.Single(forecast.Days).Precipitation.Percent);
    }

    [Fact]
    public void SkyCondition_IsTheMostFrequent()
    {
        ForecastInterval[] intervals =
        [
            Interval(new DateTimeOffset(2025, 10, 13, 6, 0, 0, TimeSpan.Zero), 8m, sky: SkyCondition.Rain),
            Interval(new DateTimeOffset(2025, 10, 13, 9, 0, 0, TimeSpan.Zero), 9m, sky: SkyCondition.Cloudy),
            Interval(new DateTimeOffset(2025, 10, 13, 12, 0, 0, TimeSpan.Zero), 10m, sky: SkyCondition.Cloudy),
        ];

        WeatherForecast forecast = WeatherForecast.FromIntervals(London, intervals, TimeSpan.Zero);

        Assert.Equal(SkyCondition.Cloudy, Assert.Single(forecast.Days).SkyCondition);
    }

    [Fact]
    public void SkyCondition_TieBreaksOnFirstOccurrence()
    {
        ForecastInterval[] intervals =
        [
            Interval(new DateTimeOffset(2025, 10, 13, 6, 0, 0, TimeSpan.Zero), 8m, sky: SkyCondition.Rain),
            Interval(new DateTimeOffset(2025, 10, 13, 9, 0, 0, TimeSpan.Zero), 9m, sky: SkyCondition.Clear),
        ];

        WeatherForecast forecast = WeatherForecast.FromIntervals(London, intervals, TimeSpan.Zero);

        Assert.Equal(SkyCondition.Rain, Assert.Single(forecast.Days).SkyCondition);
    }

    [Fact]
    public void EmptyIntervals_AreRejected()
    {
        Assert.Throws<WeatherDomainException>(() =>
            WeatherForecast.FromIntervals(London, [], TimeSpan.Zero));
    }

    [Fact]
    public void OffsetBeyondFourteenHours_IsRejected()
    {
        ForecastInterval[] intervals =
        [
            Interval(new DateTimeOffset(2025, 10, 13, 6, 0, 0, TimeSpan.Zero), 8m),
        ];

        Assert.Throws<WeatherDomainException>(() =>
            WeatherForecast.FromIntervals(London, intervals, TimeSpan.FromHours(15)));
    }
}
