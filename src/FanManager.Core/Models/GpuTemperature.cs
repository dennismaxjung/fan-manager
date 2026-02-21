namespace FanManager.Core.Models;

/// <summary>
/// GPU temperature status
/// </summary>
public sealed record GpuTemperature(
    TemperatureReading Reading,
    decimal Threshold,
    decimal? Max,
    GpuInfo? Info = null)
{
    public bool IsAboveThreshold => Reading.Celsius >= Threshold;
    public decimal DifferenceFromThreshold => Reading.Celsius - Threshold;
    public bool IsAboveMax => Max is not null && Reading.Celsius >= Max;
}
