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
            await _processService.ExecuteCommandAsync(IProcessService.Command.NvidiaSmi, "--version", cancellationToken);
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

        // Query: index,name,driver_version
        var output = await _processService.ExecuteCommandAsync(
            IProcessService.Command.NvidiaSmi,
            "--query-gpu=index,name,driver_version --format=csv,noheader",
            cancellationToken);

        var gpus = new List<GpuInfo>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split(',');
            if (parts.Length >= 3 &&
                int.TryParse(parts[0].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var index))
            {
                gpus.Add(new GpuInfo(
                    Name: parts[1].Trim(),
                    Index: index,
                    DriverVersion: parts[2].Trim()));
            }
        }

        _logger.LogInformation("Found {Count} GPU(s)", gpus.Count);
        return gpus;
    }

    public async Task<TemperatureReading> GetHighestGpuTemperatureAsync(CancellationToken cancellationToken = default)
    {
        var temperatures = await GetAllGpuTemperaturesAsync(cancellationToken);

        if (temperatures.Count == 0)
            throw new InvalidOperationException("No GPU temperatures available");

        var highest = temperatures.MaxBy(t => t.Celsius);
        _logger.LogDebug("Highest GPU temperature: {Temperature}°C", highest.Celsius);

        return highest;
    }

    public async Task<IReadOnlyList<TemperatureReading>> GetAllGpuTemperaturesAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsAvailableAsync(cancellationToken))
            return Array.Empty<TemperatureReading>();

        // Query: temperature.gpu (returns temperatures only)
        var output = await _processService.ExecuteCommandAsync(
            IProcessService.Command.NvidiaSmi,
            "--query-gpu=temperature.gpu --format=csv,noheader,nounits",
            cancellationToken);

        var temperatures = new List<TemperatureReading>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (decimal.TryParse(line.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var temp))
            {
                temperatures.Add(TemperatureReading.Now(temp));
            }
        }

        return temperatures;
    }
}
