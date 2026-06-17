using FanManager.Core.Extensions;
using FanManager.Core.Interfaces;
using FanManager.Core.Models;
using FanManager.Core.Options;
using FanManager.Core.Services;
using Microsoft.Extensions.Options;

namespace FanManager.Workers;

/// <summary>
/// Background service that monitors temperatures and controls fans
/// </summary>
public sealed class FanManagerWorker : BackgroundService
{
    private readonly ILogger<FanManagerWorker> _logger;
    private readonly IIpmiService _ipmiService;
    private readonly INvidiaSmiService _nvidiaSmiService;
    private readonly FanControlStrategy _fanControlStrategy;
    private readonly IDataStore _dataStore;
    private readonly FanManagerOptions _options;
    private bool _gpuAvailable;
    private bool _manualControlEnabled;
    private bool? _dellThirdPartyCoolingBehaviorEnabled;
    private bool _ipmiAvailable = true;

    public FanManagerWorker(
        ILogger<FanManagerWorker> logger,
        IIpmiService ipmiService,
        INvidiaSmiService nvidiaSmiService,
        FanControlStrategy fanControlStrategy,
        IDataStore dataStore,
        IOptions<FanManagerOptions> options)
    {
        _logger = logger;
        _ipmiService = ipmiService;
        _nvidiaSmiService = nvidiaSmiService;
        _fanControlStrategy = fanControlStrategy;
        _dataStore = dataStore;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("""
                               ╔════════════════════════════════════════════════════════════╗
                               ║  Fan Manager                                               ║
                               ╚════════════════════════════════════════════════════════════╝
                               """);

        try
        {
            await InitializeAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await MonitorAndControlAsync(stoppingToken);
                await Task.Delay(_options.CheckInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Fan manager shutting down...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in fan manager");
            throw;
        }
        finally
        {
            await CleanupAsync();
        }
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("""
                               === Initialization ===
                               Configuration:
                                 Base Fan Speed: {Speed}
                                 CPU Threshold: {Threshold}°C
                                 GPU Threshold: {Threshold}°C
                                 GPU Max: {MAX}°C
                                 Check Interval: {Interval}
                                 Dell third-party PCIe card cooling behavior: {Behavior}
                                 Restore Dell third-party PCIe card cooling behavior on exit: {Behavior}
                               """, _options.BaseFanSpeed, _options.CpuTemperatureThreshold,
            _options.GpuTemperatureThreshold, _options.GpuTemperatureMax, _options.CheckInterval,
            _options.EnableDellThirdPartyPcieCardCoolingBehavior.ToEnabledDisabled(),
            _options.RestoreDellThirdPartyPcieCardCoolingBehaviorOnExit.ToEnabledDisabled()
        );

        // Check IPMI availability
        if (!await _ipmiService.IsAvailableAsync(cancellationToken))
        {
            _ipmiAvailable = false;
            throw new InvalidOperationException("IPMI is not available!");
        }
        _logger.LogInformation("✓ IPMI available");

        _dellThirdPartyCoolingBehaviorEnabled =
            await _ipmiService.GetCurrentDellThirdPartyCoolingBehaviourAsync(cancellationToken);
        _logger.LogInformation("Dell third-party PCIe card cooling behavior currently: {Behavior}",
            _dellThirdPartyCoolingBehaviorEnabled.ToEnabledDisabled());

        if (_dellThirdPartyCoolingBehaviorEnabled != _options.EnableDellThirdPartyPcieCardCoolingBehavior)
        {
            await _ipmiService.SetDellThirdPartyCoolingBehaviourAsync(_options.EnableDellThirdPartyPcieCardCoolingBehavior,
                cancellationToken);
            _logger.LogInformation("Dell third-party PCIe card cooling behavior now: {Behavior}",
                _options.EnableDellThirdPartyPcieCardCoolingBehavior.ToEnabledDisabled());
        }

        // Check GPU availability
        _gpuAvailable = await _nvidiaSmiService.IsAvailableAsync(cancellationToken);
        if (_gpuAvailable)
        {
            var gpus = await _dataStore.GetGpuInfosAsync(cancellationToken);
            _logger.LogInformation("✓ {Count} GPU(s) detected:", gpus.Count);
            foreach (var gpu in gpus)
            {
                _logger.LogInformation("  GPU {Id}: {Name} (Driver: {Driver}, CoolingType: {CoolingType})",
                    gpu.Id, gpu.Name, gpu.DriverVersion, gpu.CoolingType.ToString());
            }
        }
        else
        {
            _logger.LogWarning("GPU monitoring disabled - nvidia-smi not available");
        }

        // Enable manual fan control
        await _ipmiService.EnableManualFanControlAsync(cancellationToken);
        await _ipmiService.SetFanSpeedAsync(_options.BaseFanSpeed, cancellationToken);
        _manualControlEnabled = true;

        _logger.LogInformation("=== Monitoring started ===\n");
    }

    private async Task MonitorAndControlAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Read temperatures
            var cpuTemp = await _ipmiService.GetCpuTemperatureAsync(cancellationToken);

            GpuTemperature? gpuTemp = null;
            if (_gpuAvailable)
            {
                var gpuInfos = await _dataStore.GetGpuInfosAsync(cancellationToken);
                var gpuReading = (await _nvidiaSmiService.GetAllGpuTemperaturesAsync(cancellationToken))
                    .Where(temp => gpuInfos.Any(gpuInfo => gpuInfo.Id == temp.Key && gpuInfo.CoolingType == GpuCoolingType.Passive))
                    .ToDictionary()
                    .GetHighestTemperature();
                gpuTemp = new GpuTemperature(gpuReading.Value, _options.GpuTemperatureThreshold, gpuInfos.First(gpu => gpu.Id == gpuReading.Key), _options.GpuTemperatureMax);
            }

            var status = new SystemTemperatureStatus(
                Cpu: new CpuTemperature(cpuTemp, _options.CpuTemperatureThreshold),
                Gpu: gpuTemp,
                Timestamp: DateTimeOffset.Now);

            // Calculate fan control decision
            var decision = _fanControlStrategy.Calculate(status);

            // Log status
            LogStatus(status, decision);

            // Apply decision
            await ApplyDecisionAsync(decision, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during monitoring cycle");
        }
    }

    private void LogStatus(SystemTemperatureStatus status, FanControlDecision decision)
    {
        var gpuMax = status.Gpu is not null ? $", Max: {status.Gpu.Max}°C" : "";
        _logger.LogInformation("""
                               ========================================================
                               Temperature Status - {Timestamp:yyyy-MM-dd HH:mm:ss}
                               ========================================================
                               CPU Temperature: {CpuTemp:F1}°C (Threshold: {CpuThreshold:F1}°C)
                               {GpuStatus}
                               --------------------------------------------------------
                               Decision: {DecisionType}
                               {DecisionDetails}
                               ========================================================

                               """,
            status.Timestamp,
            status.Cpu.Reading.Celsius,
            status.Cpu.Threshold,
            status.Gpu is not null
                ? $"GPU Temperature: {status.Gpu.Reading.Celsius:F1}°C (Threshold: {status.Gpu.Threshold:F1}°C{gpuMax}) Id: {status.Gpu.Info.Id}"
                : "GPU: Not available",
            decision.GetType().Name,
            FormatDecisionDetails(decision));
    }

    private static string FormatDecisionDetails(FanControlDecision decision) => decision switch
    {
        FanControlDecision.ManualControl mc =>
            $"Manual control at {mc.Speed}",

        FanControlDecision.DellAutoControl dac =>
            $"⚠ Dell iDRAC control:\n\t{dac.Reason}",

        FanControlDecision.DynamicGpuControl dgc =>
            $"🌡 Dynamic GPU control: Fan speed → {dgc.AdjustedSpeed}\n\t {dgc.Reason}",

        FanControlDecision.MaxFanSpeed dgc =>
            $"🌡 Dynamic GPU control: Fan speed → {dgc.AdjustedSpeed}\n\t {dgc.Reason}",

        _ => decision.ToString()
    };

    private async Task ApplyDecisionAsync(FanControlDecision decision, CancellationToken cancellationToken)
    {
        switch (decision)
        {
            case FanControlDecision.ManualControl mc:
                if (!_manualControlEnabled)
                {
                    await _ipmiService.EnableManualFanControlAsync(cancellationToken);
                    _manualControlEnabled = true;
                }

                await _ipmiService.SetFanSpeedAsync(mc.Speed, cancellationToken);
                break;

            case FanControlDecision.DellAutoControl:
                if (_manualControlEnabled)
                {
                    await _ipmiService.DisableManualFanControlAsync(cancellationToken);
                    _manualControlEnabled = false;
                }

                break;

            case FanControlDecision.DynamicGpuControl dgc:
                if (!_manualControlEnabled)
                {
                    await _ipmiService.EnableManualFanControlAsync(cancellationToken);
                    _manualControlEnabled = true;
                }

                await _ipmiService.SetFanSpeedAsync(dgc.AdjustedSpeed, cancellationToken);
                break;
            case FanControlDecision.MaxFanSpeed mfs:
                if (!_manualControlEnabled)
                {
                    await _ipmiService.EnableManualFanControlAsync(cancellationToken);
                    _manualControlEnabled = true;
                }

                await _ipmiService.SetFanSpeedAsync(mfs.AdjustedSpeed, cancellationToken);
                break;
        }
    }

    private async Task CleanupAsync()
    {
        if (!_ipmiAvailable)
            return;
        try
        {
            _logger.LogInformation("Cleanup: Disabling manual fan control");
            await _ipmiService.DisableManualFanControlAsync(CancellationToken.None);

            if (_options.RestoreDellThirdPartyPcieCardCoolingBehaviorOnExit && _dellThirdPartyCoolingBehaviorEnabled.HasValue)
            {
                _logger.LogInformation("Cleanup: Restore Dell third-party PCIe card cooling behavior");
                await _ipmiService.SetDellThirdPartyCoolingBehaviourAsync(_dellThirdPartyCoolingBehaviorEnabled.Value,
                    CancellationToken.None);
            }

            _logger.LogInformation("Cleanup: Done");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup");
        }
    }
}
