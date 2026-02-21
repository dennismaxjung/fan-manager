using FanManager.Core.Models;

namespace FanManager.Core.Interfaces;

/// <summary>
/// Interface for IPMI tool interactions
/// Allows mocking for unit tests
/// </summary>
public interface IIpmiService
{
    /// <summary>
    /// Reads current CPU temperature from iDRAC
    /// </summary>
    Task<TemperatureReading> GetCpuTemperatureAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables manual fan control mode
    /// </summary>
    Task EnableManualFanControlAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables manual fan control (Dell iDRAC takes over)
    /// </summary>
    Task DisableManualFanControlAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets fan speed percentage
    /// </summary>
    Task SetFanSpeedAsync(FanSpeedPercentage speed, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if IPMI tool is available and working
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current Dell third-party PCIe card cooling behavior.
    /// Indicates whether the cooling response for third-party PCIe cards is enabled or disabled.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a boolean value:
    /// true if the Dell third-party PCIe card cooling response is disabled;
    /// false if it is enabled.
    /// </returns>
    public Task<bool> GetCurrentDellThirdPartyCoolingBehaviourAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures the Dell third-party cooling behavior for PCIe cards.
    /// </summary>
    /// <param name="enable">Specifies whether to enable or disable the third-party cooling behavior.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SetDellThirdPartyCoolingBehaviourAsync(bool enable, CancellationToken cancellationToken = default);
}
