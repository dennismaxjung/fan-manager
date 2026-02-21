namespace FanManager.Core.Models;

/// <summary>
/// Complete system temperature status
/// </summary>
public sealed record SystemTemperatureStatus(
    CpuTemperature Cpu,
    GpuTemperature? Gpu,
    DateTimeOffset Timestamp)
{
    /// <summary>
    /// Requires Dell iDRAC to take control (safety fallback)
    /// </summary>
    public bool RequiresDellControl =>
        Cpu.IsAboveThreshold &&
        (Gpu is null || !Gpu.IsAboveMax);

    /// <summary>
    /// Requires dynamic GPU-based fan control
    /// </summary>
    public bool RequiresDynamicGpuControl =>
        !Cpu.IsAboveThreshold &&
        Gpu is not null &&
        Gpu.IsAboveThreshold &&
        !Gpu.IsAboveMax;

    /// <summary>
    /// Requires full  GPU-based fan control
    /// </summary>
    public bool RequiresMaxFanSpeed =>
        Gpu is not null && Gpu.IsAboveMax;

    /// <summary>
    /// All temperatures within the normal range
    /// </summary>
    public bool IsNormal =>
        !Cpu.IsAboveThreshold &&
        (Gpu is null || !Gpu.IsAboveThreshold && !Gpu.IsAboveMax);
}
