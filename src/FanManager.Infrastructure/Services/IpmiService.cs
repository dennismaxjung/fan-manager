using System.Globalization;
using System.Text.RegularExpressions;
using FanManager.Core.Interfaces;
using FanManager.Core.Models;
using FanManager.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FanManager.Infrastructure.Services;

/// <summary>
/// IPMI service implementation using ipmitool command
/// </summary>
public sealed partial class IpmiService : IIpmiService
{
    private readonly ILogger<IpmiService> _logger;
    private readonly IProcessService _processService;
    private readonly IpmiOptions _config;

    public IpmiService(ILogger<IpmiService> logger, IOptions<IpmiOptions> options, IProcessService processService)
    {
        _logger = logger;
        _processService = processService;
        _config = options.Value;
    }

    public async Task<TemperatureReading> GetCpuTemperatureAsync(CancellationToken cancellationToken = default)
    {
        var args = BuildIpmiArgs("sdr type temperature");
        var output = await _processService.ExecuteCommandAsync(IProcessService.Command.IpmiTool, args, cancellationToken);

        // Real output contains multiple sensors, e.g.
        // Inlet Temp | ... | 19 degrees C
        // Exhaust Temp | ... | 29 degrees C
        // Temp | ... | 34 degrees C
        // Temp | ... | 32 degrees C
        //
        // We treat CPU temperature as the highest reading among the generic "Temp" sensors
        // (excluding "Inlet Temp" / "Exhaust Temp").
        var matches = CpuTempLineRegex().Matches(output);

        decimal? maxTemp = null;
        foreach (Match match in matches)
        {
            if (!match.Success)
            {
                continue;
            }

            var value = match.Groups["temp"].Value;
            if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var temperature))
            {
                continue;
            }

            maxTemp = maxTemp is null ? temperature : Math.Max(maxTemp.Value, temperature);
        }

        if (maxTemp is null)
        {
            _logger.LogError("Failed to parse CPU temperature from output: {Output}", output);
            throw new InvalidOperationException("Could not read CPU temperature");
        }

        _logger.LogDebug("CPU temperature: {Temperature}°C", maxTemp.Value);
        return TemperatureReading.Now(maxTemp.Value);
    }

    public async Task EnableManualFanControlAsync(CancellationToken cancellationToken = default)
    {
        var args = BuildIpmiArgs("raw 0x30 0x30 0x01 0x00");
        await _processService.ExecuteCommandAsync(IProcessService.Command.IpmiTool, args, cancellationToken);
        _logger.LogDebug("Manual fan control enabled");
    }

    public async Task DisableManualFanControlAsync(CancellationToken cancellationToken = default)
    {
        var args = BuildIpmiArgs("raw 0x30 0x30 0x01 0x01");
        await _processService.ExecuteCommandAsync(IProcessService.Command.IpmiTool, args, cancellationToken);
        _logger.LogDebug("Manual fan control disabled - Dell iDRAC takes over");
    }

    public async Task SetFanSpeedAsync(FanSpeedPercentage speed, CancellationToken cancellationToken = default)
    {
        var hexSpeed = $"0x{speed.Value:X2}";
        var args = BuildIpmiArgs($"raw 0x30 0x30 0x02 0xff {hexSpeed}");
        await _processService.ExecuteCommandAsync(IProcessService.Command.IpmiTool, args, cancellationToken);
        _logger.LogDebug("Fan speed set to {Speed}", speed);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var args = BuildIpmiArgs("sdr type temperature");
            await _processService.ExecuteCommandAsync(IProcessService.Command.IpmiTool, args, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> GetCurrentDellThirdPartyCoolingBehaviourAsync(CancellationToken cancellationToken = default)
    {
        var args = BuildIpmiArgs("raw 0x30 0xce 0x01 0x16 0x05 0x00 0x00 0x00");
        var output = await _processService.ExecuteCommandAsync(IProcessService.Command.IpmiTool, args, cancellationToken);

        var normalizedOutput = output.Trim().ToUpperInvariant();

        switch (normalizedOutput)
        {
            // Check if third-party PCIe card cooling response is DISABLED
            case "16 05 00 00 00 05 00 01 00 00":
                _logger.LogDebug("Dell third-party PCIe card cooling response is DISABLED");
                return false;
            // Check if third-party PCIe card cooling response is ENABLED
            case "16 05 00 00 00 05 00 00 00 00":
                _logger.LogDebug("Dell third-party PCIe card cooling response is ENABLED");
                return true;
            default:
                // Unexpected output
                _logger.LogError("Unexpected third-party PCIe card cooling response output: {Output}", output);
                throw new InvalidOperationException($"Unexpected third-party PCIe card cooling response: {output}");
        }
    }

    public async Task SetDellThirdPartyCoolingBehaviourAsync(bool enable,
        CancellationToken cancellationToken = default)
    {
        var state = enable ? "0x00" : "0x01";
        var args = BuildIpmiArgs($"raw 0x30 0xce 0x00 0x16 0x05 0x00 0x00 0x00 0x05 0x00 {state} 0x00 0x00");
        await _processService.ExecuteCommandAsync(IProcessService.Command.IpmiTool, args, cancellationToken);
        _logger.LogDebug("DELL Third-Party PCIe-Card cooling behavior set to: {Speed}", enable);
    }

    private string BuildIpmiArgs(string command)
    {
        return _config.Host.Equals("local", StringComparison.OrdinalIgnoreCase)
            ? command
            : $"-I lanplus -H {_config.Host} -U {_config.Username} -P {_config.Password} {command}";
    }

    [GeneratedRegex(
        @"(?m)^\s*Temp\s*\|.*?\|\s*(?<temp>\d+(?:\.\d+)?)\s+degrees\s+C\s*$",
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex CpuTempLineRegex();
}
