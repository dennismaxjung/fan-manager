using FanManager.Core.Models;
using Microsoft.Extensions.Configuration;

namespace FanManager.Core.Options;

/// <summary>
/// Worker configuration options
/// </summary>
public sealed record FanManagerOptions
{
    [ConfigurationKeyName("BASE_FAN_SPEED")]
    public FanSpeedPercentage BaseFanSpeed { get; init; } = new(20);

    [ConfigurationKeyName("CPU_TEMPERATURE_THRESHOLD")]
    public decimal CpuTemperatureThreshold { get; init; } = 60;

    [ConfigurationKeyName("GPU_TEMPERATURE_THRESHOLD")]
    public decimal GpuTemperatureThreshold { get; init; } = 45;

    [ConfigurationKeyName("GPU_TEMPERATURE_MAX")]
    public decimal? GpuTemperatureMax { get; init; }

    [ConfigurationKeyName("CHECK_INTERVAL")]
    public TimeSpan CheckInterval { get; init; } = TimeSpan.FromSeconds(10);

    [ConfigurationKeyName("ENABLE_DELL_THIRD_PARTY_PCIE_CARD_COOLING_BEHAVIOR")]
    public bool EnableDellThirdPartyPcieCardCoolingBehavior { get; init; } = false;

    [ConfigurationKeyName("RESTORE_DELL_THIRD_PARTY_PCIE_CARD_COOLING_BEHAVIOR_ON_EXIT")]
    public bool RestoreDellThirdPartyPcieCardCoolingBehaviorOnExit { get; init; } = true;
}
