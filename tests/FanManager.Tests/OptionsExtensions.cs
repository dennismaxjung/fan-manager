using FanManager.Core.Models;
using FanManager.Core.Options;
using Microsoft.Extensions.Options;

namespace FanManager.Tests;

public static class OptionsExtensions
{
    extension(IOptions<FanManagerOptions> options)
    {
        public SystemTemperatureStatus CreateSystemTemperatureStatus(decimal cpuTemperature, decimal gpuTemperature) =>
            new(
                Cpu: new CpuTemperature(TemperatureReading.Now(cpuTemperature), options.Value.CpuTemperatureThreshold),
                Gpu: new GpuTemperature(TemperatureReading.Now(gpuTemperature), options.Value.GpuTemperatureThreshold,
                    new GpuInfo(TestConstants.FirstGuid, "Fake GPU", "000.000.000", GpuCoolingType.Passive),
                    options.Value.GpuTemperatureMax),
                Timestamp: DateTimeOffset.Now);

        public SystemTemperatureStatus CreateSystemTemperatureStatus(decimal cpuTemperature) =>
            new(
                Cpu: new CpuTemperature(TemperatureReading.Now(cpuTemperature), options.Value.CpuTemperatureThreshold),
                Gpu: null,
                Timestamp: DateTimeOffset.Now);
    }
}
