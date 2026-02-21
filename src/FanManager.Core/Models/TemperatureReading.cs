using System.Runtime.InteropServices;

namespace FanManager.Core.Models;

/// <summary>
/// Temperature reading with timestamp
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct TemperatureReading(
    decimal Celsius,
    DateTimeOffset Timestamp)
{
    public static TemperatureReading Now(decimal celsius) =>
        new(celsius, DateTimeOffset.Now);
}
