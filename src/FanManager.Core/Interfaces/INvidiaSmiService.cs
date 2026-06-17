using FanManager.Core.Models;

namespace FanManager.Core.Interfaces;

/// <summary>
/// Interface for nvidia-smi interactions
/// Allows mocking for unit tests
/// </summary>
public interface INvidiaSmiService
{
    /// <summary>
    /// Checks if nvidia-smi is available and GPUs are detected
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about all available GPUs
    /// </summary>
    Task<IReadOnlyList<GpuInfo>> GetGpuInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads temperatures from all GPUs individually
    /// </summary>
    Task<IReadOnlyDictionary<Guid, TemperatureReading>> GetAllGpuTemperaturesAsync(CancellationToken cancellationToken = default);
}
