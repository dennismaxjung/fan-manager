namespace FanManager.Core.Models;

/// <summary>
/// GPU temperature status
/// </summary>
public sealed record GpuTemperature(
    TemperatureReading Reading,
    decimal Threshold,
    GpuInfo Info,
    decimal? Max)
{
    public bool IsAboveThreshold => Reading.Celsius >= Threshold;
    public decimal DifferenceFromThreshold => Reading.Celsius - Threshold;
    public bool IsAboveMax => Max is not null && Reading.Celsius >= Max;
}
