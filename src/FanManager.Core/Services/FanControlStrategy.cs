using FanManager.Core.Models;
using FanManager.Core.Options;
using Microsoft.Extensions.Options;

namespace FanManager.Core.Services;

/// <summary>
/// Calculates fan speed based on temperature status
/// Pure business logic - no external dependencies
/// </summary>
public sealed class FanControlStrategy
{
    private readonly FanManagerOptions _options;

    public FanControlStrategy(IOptions<FanManagerOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Calculates required fan control action based on system temperature
    /// </summary>
    public FanControlDecision Calculate(SystemTemperatureStatus status)
    {
        var timestamp = DateTimeOffset.Now;

        // Priority 1: CPU protection (safety first!)
        if (status.RequiresDellControl)
        {
            return new FanControlDecision.DellAutoControl(
                Reason:
                $"CPU temperature {status.Cpu.Reading.Celsius:F1}°C exceeds threshold {status.Cpu.Threshold:F1}°C",
                Timestamp: timestamp);
        }

        // Priority 2: Dynamic GPU control
        if (status is { RequiresDynamicGpuControl: true, Gpu: not null })
        {
            var adjustedSpeed = CalculateDynamicGpuFanSpeed(
                status.Gpu,
                _options.BaseFanSpeed);

            return new FanControlDecision.DynamicGpuControl(
                AdjustedSpeed: adjustedSpeed,
                Reason: $"GPU temperature {status.Gpu.Reading.Celsius:F1}°C exceeds threshold {status.Gpu.Threshold:F1}°C",
                Timestamp: timestamp);
        }

        if (status is { RequiresMaxFanSpeed: true, Gpu: not null })
        {
            return new FanControlDecision.MaxFanSpeed(
                Reason: $"GPU temperature {status.Gpu.Reading.Celsius:F1}°C exceeds max {status.Gpu.Max:F1}°C",
                Timestamp: timestamp);
        }

        // Priority 3: Normal operation - manual control at base speed
        return new FanControlDecision.ManualControl(
            Speed: _options.BaseFanSpeed,
            Timestamp: timestamp);
    }

    /// <summary>
    /// Calculates dynamic fan speed increase based on GPU temperature
    /// Formula: For every X°C over the threshold, increase fan speed by Y%
    /// </summary>
    private FanSpeedPercentage CalculateDynamicGpuFanSpeed(
        GpuTemperature gpu,
        FanSpeedPercentage baseSpeed)
    {
        if (!gpu.IsAboveThreshold)
            return baseSpeed;

        // Defensive: avoid division by zero / invalid configuration
        if (gpu.Max is not null && gpu.Max <= gpu.Threshold)
            return new FanSpeedPercentage(100);

        if (gpu.IsAboveMax)
            return new FanSpeedPercentage(100);

        if (gpu.Max is null)
        {
            var tempOver = gpu.DifferenceFromThreshold;

            // Aggressive formula for passive cooling:
            // Every 3°C over the threshold = +15% fan speed
            var increasePercentage = (int)Math.Floor(tempOver / 3) * 15;

            // Cap maximum increase at 60%
            increasePercentage = Math.Min(increasePercentage, 60);

            var newSpeed = baseSpeed.Value + increasePercentage;

            // Cap at 100%
            newSpeed = Math.Min(newSpeed, 100);

            return new FanSpeedPercentage(newSpeed);
        }
        else
        {
            var span = gpu.Max.Value - gpu.Threshold; // > 0
            var over = gpu.Reading.Celsius - gpu.Threshold; // >= 0
            var ratio = over / span; // 0..1

            var baseValue = baseSpeed.Value;
            var interpolated = baseValue + (int)Math.Round((100 - baseValue) * ratio, MidpointRounding.AwayFromZero);

            interpolated = Math.Clamp(interpolated, baseValue, 100);
            return new FanSpeedPercentage(interpolated);
        }
    }
}
