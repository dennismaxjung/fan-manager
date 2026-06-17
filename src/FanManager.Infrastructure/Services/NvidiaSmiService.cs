using System.Diagnostics;
using System.Globalization;
using FanManager.Core.Interfaces;
using FanManager.Core.Models;
using Microsoft.Extensions.Logging;

namespace FanManager.Infrastructure.Services;

/// <summary>
/// Nvidia SMI service implementation using nvidia-smi command
/// </summary>
public sealed class NvidiaSmiService : INvidiaSmiService
{
    private readonly ILogger<NvidiaSmiService> _logger;
    private readonly IProcessService _processService;
    private bool? _isAvailable;

    public NvidiaSmiService(ILogger<NvidiaSmiService> logger, IProcessService processService)
    {
        _logger = logger;
        _processService = processService;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (_isAvailable.HasValue)
            return _isAvailable.Value;

        try
        {
            await _processService.ExecuteCommandAsync(IProcessService.Command.NvidiaSmi, "--version",
                cancellationToken);
            _isAvailable = true;
            _logger.LogInformation("nvidia-smi is available");
            return true;
        }
        catch
        {
            _isAvailable = false;
            _logger.LogWarning("nvidia-smi is not available - GPU monitoring disabled");
            return false;
        }
    }

    public async Task<IReadOnlyList<GpuInfo>> GetGpuInfoAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsAvailableAsync(cancellationToken))
            return Array.Empty<GpuInfo>();

        // Query: uuid,name,driver_version,fan.speed
        var output = await _processService.ExecuteCommandAsync(
            IProcessService.Command.NvidiaSmi,
            "--query-gpu=uuid,name,driver_version,fan.speed --format=csv,noheader,nounits",
            cancellationToken);

        var gpus = new List<GpuInfo>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split(',');
            if (parts.Length >= 4 &&
                Guid.TryParse(parts[0].Trim().Replace("GPU-", ""), CultureInfo.InvariantCulture, out var id)
               )
            {
                gpus.Add(new GpuInfo(
                    id,
                    Name: parts[1].Trim(),
                    DriverVersion: parts[2].Trim(),
                    string.Equals(parts[3].Trim(), "[N/A]", StringComparison.OrdinalIgnoreCase)
                        ? GpuCoolingType.Passive
                        : GpuCoolingType.Active));
            }
        }

        _logger.LogInformation("Found {Count} GPU(s)", gpus.Count);
        return gpus;
    }

    public async Task<IReadOnlyDictionary<Guid, TemperatureReading>> GetAllGpuTemperaturesAsync(
        CancellationToken cancellationToken = default)
    {
        if (!await IsAvailableAsync(cancellationToken))
            return new Dictionary<Guid, TemperatureReading>();

        // Query: uuid,temperature.gpu (returns temperatures only)
        var output = await _processService.ExecuteCommandAsync(
            IProcessService.Command.NvidiaSmi,
            "--query-gpu=uuid,temperature.gpu --format=csv,noheader,nounits",
            cancellationToken);

        var temperatures = new Dictionary<Guid, TemperatureReading>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split(',');

            if (parts.Length >= 2 &&
                Guid.TryParse(parts[0].Trim().Replace("GPU-", ""), CultureInfo.InvariantCulture, out var id) &&
                decimal.TryParse(parts[1].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var temp))
            {
                temperatures.Add(id, TemperatureReading.Now(temp));
            }
        }

        return temperatures;
    }
}
