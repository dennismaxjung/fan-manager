namespace FanManager.Core.Models;

/// <summary>
/// CPU temperature status
/// </summary>
public sealed record CpuTemperature(
    TemperatureReading Reading,
    decimal Threshold)
{
    public bool IsAboveThreshold => Reading.Celsius >= Threshold;
    public decimal DifferenceFromThreshold => Reading.Celsius - Threshold;
}
