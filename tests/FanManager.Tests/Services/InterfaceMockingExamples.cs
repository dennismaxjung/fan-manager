using FanManager.Core.Interfaces;
using FanManager.Core.Models;

namespace FanManager.Tests.Services;

/// <summary>
/// Example tests demonstrating how to mock IIpmiService and INvidiaSmiService
/// This shows the power of interface-based design for testability
/// </summary>
public sealed class InterfaceMockingExamples
{
    [Fact]
    public async Task IIpmiService_CanBeMockedAsync()
    {
        // Arrange
        var mockIpmiService = new Mock<IIpmiService>();
        mockIpmiService
            .Setup(x => x.GetCpuTemperatureAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TemperatureReading.Now(45));

        mockIpmiService
            .Setup(x => x.SetFanSpeedAsync(It.IsAny<FanSpeedPercentage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var temperature = await mockIpmiService.Object.GetCpuTemperatureAsync();
        await mockIpmiService.Object.SetFanSpeedAsync(new FanSpeedPercentage(30));

        // Assert
        temperature.Celsius.Should().Be(45);
        mockIpmiService.Verify(x =>
                x.SetFanSpeedAsync(It.Is<FanSpeedPercentage>(s => s.Value == 30), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task INvidiaSmiService_CanBeMockedAsync()
    {
        // Arrange
        var mockNvidiaService = new Mock<INvidiaSmiService>();

        mockNvidiaService
            .Setup(x => x.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockNvidiaService
            .Setup(x => x.GetGpuInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GpuInfo>
            {
                new(TestConstants.FirstGuid, "NVIDIA Quadro P2000", "535.183.01", GpuCoolingType.Active)
            });

        mockNvidiaService
            .Setup(x => x.GetAllGpuTemperaturesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, TemperatureReading> { { TestConstants.FirstGuid, TemperatureReading.Now(65) } });

        // Act
        var isAvailable = await mockNvidiaService.Object.IsAvailableAsync();
        var gpus = await mockNvidiaService.Object.GetGpuInfoAsync();
        var temperatures = await mockNvidiaService.Object.GetAllGpuTemperaturesAsync();

        // Assert
        isAvailable.Should().BeTrue();
        gpus.Should().HaveCount(1);
        gpus[0].Name.Should().Be("NVIDIA Quadro P2000");
        temperatures.Count.Should().Be(1);
        temperatures.First().Key.Should().Be(TestConstants.FirstGuid);
        temperatures.First().Value.Celsius.Should().Be(65);
    }

    [Fact]
    public async Task CombinedScenario_BothServicesMockedAsync()
    {
        // Arrange
        var mockIpmi = new Mock<IIpmiService>();
        var mockNvidia = new Mock<INvidiaSmiService>();

        // Setup CPU temperature
        mockIpmi
            .Setup(x => x.GetCpuTemperatureAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TemperatureReading.Now(55));

        // Setup GPU temperature (above threshold)
        mockNvidia
            .Setup(x => x.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockNvidia
            .Setup(x => x.GetAllGpuTemperaturesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, TemperatureReading> { { TestConstants.FirstGuid, TemperatureReading.Now(75) } });

        // Setup IPMI commands
        mockIpmi
            .Setup(x => x.EnableManualFanControlAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockIpmi
            .Setup(x => x.SetFanSpeedAsync(It.IsAny<FanSpeedPercentage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var cpuTemp = await mockIpmi.Object.GetCpuTemperatureAsync();
        var gpuTemps = await mockNvidia.Object.GetAllGpuTemperaturesAsync();

        await mockIpmi.Object.EnableManualFanControlAsync();
        await mockIpmi.Object.SetFanSpeedAsync(new FanSpeedPercentage(40));

        // Assert
        cpuTemp.Celsius.Should().Be(55);
        gpuTemps.Count.Should().Be(1);
        gpuTemps.First().Key.Should().Be(TestConstants.FirstGuid);
        gpuTemps.First().Value.Celsius.Should().Be(75);

        mockIpmi.Verify(x => x.EnableManualFanControlAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockIpmi.Verify(x => x.SetFanSpeedAsync(
                It.Is<FanSpeedPercentage>(s => s.Value == 40),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SimulateGpuUnavailable_ScenarioAsync()
    {
        // Arrange - GPU not available
        var mockNvidia = new Mock<INvidiaSmiService>();
        mockNvidia
            .Setup(x => x.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var isAvailable = await mockNvidia.Object.IsAvailableAsync();

        // Assert
        isAvailable.Should().BeFalse();

        // Verify GetGpuInfoAsync is never called when unavailable
        mockNvidia.Verify(x => x.GetGpuInfoAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
