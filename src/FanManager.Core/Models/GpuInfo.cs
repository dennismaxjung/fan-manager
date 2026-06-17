namespace FanManager.Core.Models;

/// <summary>
/// GPU information and temperature
/// </summary>
public sealed record GpuInfo(
    Guid Id,
    string Name,
    string DriverVersion,
    GpuCoolingType CoolingType);
