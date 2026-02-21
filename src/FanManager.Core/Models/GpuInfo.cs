namespace FanManager.Core.Models;

/// <summary>
/// GPU information and temperature
/// </summary>
public sealed record GpuInfo(
    string Name,
    int Index,
    string DriverVersion);
