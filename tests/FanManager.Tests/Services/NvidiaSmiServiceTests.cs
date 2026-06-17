using FanManager.Core.Extensions;
using FanManager.Core.Interfaces;
using FanManager.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace FanManager.Tests.Services;

public sealed class NvidiaSmiServiceTests
{
    private readonly Mock<IProcessService> _process = new();
    private readonly Mock<ILogger<NvidiaSmiService>> _logger = new();

    private NvidiaSmiService CreateSut()
        => new(_logger.Object, _process.Object);

    [Fact]
    public async Task IsAvailableAsync_WhenCommandSucceeds_ReturnsTrue_AndCachesAsync()
    {
        // Arrange (real-world output)
        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.NvidiaSmi,
                "--version",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                         NVIDIA-SMI version  : 580.126.16
                         NVML version        : 580.126
                         DRIVER version      : 580.126.16
                         CUDA Version        : 13.0
                         """);

        var sut = CreateSut();

        // Act
        var first = await sut.IsAvailableAsync();
        var second = await sut.IsAvailableAsync();

        // Assert
        first.Should().BeTrue();
        second.Should().BeTrue();

        _process.Verify(p => p.ExecuteCommandAsync(
                IProcessService.Command.NvidiaSmi,
                "--version",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IsAvailableAsync_WhenCommandThrows_ReturnsFalse_AndCachesAsync()
    {
        // Arrange
        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.NvidiaSmi,
                "--version",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("nvidia-smi not installed"));

        var sut = CreateSut();

        // Act
        var first = await sut.IsAvailableAsync();
        var second = await sut.IsAvailableAsync();

        // Assert
        first.Should().BeFalse();
        second.Should().BeFalse();

        _process.Verify(p => p.ExecuteCommandAsync(
                IProcessService.Command.NvidiaSmi,
                "--version",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetGpuInfoAsync_WhenUnavailable_ReturnsEmpty_AndDoesNotQueryAsync()
    {
        // Arrange
        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.NvidiaSmi,
                "--version",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("nvidia-smi not installed"));

        var sut = CreateSut();

        // Act
        var result = await sut.GetGpuInfoAsync();

        // Assert
        result.Should().BeEmpty();

        _process.Verify(p => p.ExecuteCommandAsync(
                IProcessService.Command.NvidiaSmi,
                "--query-gpu=uuid,,name,driver_version,fan.speed --format=csv,noheader,nounits",
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetGpuInfoAsync_ParsesCsvLines_FromRealSystemSampleAsync()
    {
        // Arrange (real-world output)
        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.NvidiaSmi,
                "--version",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                         NVIDIA-SMI version  : 580.126.16
                         NVML version        : 580.126
                         DRIVER version      : 580.126.16
                         CUDA Version        : 13.0
                         """);

        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.NvidiaSmi,
                "--query-gpu=uuid,name,driver_version,fan.speed --format=csv,noheader,nounits",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync($"""
                         GPU-{TestConstants.FirstGuid}, Tesla T10, 580.126.16, [N/A]
                         GPU-{TestConstants.SecondGuid}, Tesla T10, 580.126.16, 20
                         """);

        var sut = CreateSut();

        // Act
        var gpus = await sut.GetGpuInfoAsync();

        // Assert
        gpus.Should().HaveCount(2);
        gpus[0].Id.Should().Be(TestConstants.FirstGuid);
        gpus[0].Name.Should().Be("Tesla T10");
        gpus[0].DriverVersion.Should().Be("580.126.16");
        gpus[1].Id.Should().Be(TestConstants.SecondGuid);
    }

    [Fact]
    public async Task GetAllGpuTemperaturesAsync_ParsesTemperatures_FromRealSystemSampleAsync()
    {
        // Arrange (real-world output)
        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.NvidiaSmi,
                "--version",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                         NVIDIA-SMI version  : 580.126.16
                         NVML version        : 580.126
                         DRIVER version      : 580.126.16
                         CUDA Version        : 13.0
                         """);

        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.NvidiaSmi,
                "--query-gpu=uuid,temperature.gpu --format=csv,noheader,nounits",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync($"""
                         GPU-{TestConstants.FirstGuid}, 38
                         GPU-{TestConstants.SecondGuid}, 45
                         """);

        var sut = CreateSut();

        // Act
        var temps = await sut.GetAllGpuTemperaturesAsync();

        // Assert
        temps.Should().HaveCount(2);
        temps.Select(t => t.Value.Celsius).Should().BeEquivalentTo([38m, 45m], o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task GetAllGpuTemperaturesAsync_ParsesTemperatures_IgnoresInvalidLinesAsync()
    {
        // Arrange
        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.NvidiaSmi,
                "--version",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("nvidia-smi 535.x");

        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.NvidiaSmi,
                "--query-gpu=uuid,temperature.gpu --format=csv,noheader,nounits",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync($"""
                          GPU-{TestConstants.FirstGuid}, 45
                          GPU-{TestConstants.ThirdGuid}, not-a-number
                          GPU-{TestConstants.SecondGuid}, 67
                          """);

        var sut = CreateSut();

        // Act
        var temps = await sut.GetAllGpuTemperaturesAsync();

        // Assert
        temps.Select(t => t.Value.Celsius).Should().BeEquivalentTo([45m, 67m], o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task GetHighestGpuTemperature_ReturnsMaxTemperatureAsync()
    {
        // Arrange
        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.NvidiaSmi,
                "--version",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                         NVIDIA-SMI version  : 580.126.16
                         NVML version        : 580.126
                         DRIVER version      : 580.126.16
                         CUDA Version        : 13.0
                         """);

        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.NvidiaSmi,
                "--query-gpu=uuid,temperature.gpu --format=csv,noheader,nounits",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync($"""
                         GPU-{TestConstants.FirstGuid}, 55
                         GPU-{TestConstants.SecondGuid}, 72
                         GPU-{TestConstants.ThirdGuid}, 61
                         """);

        var sut = CreateSut();

        // Act
        var highest = (await sut.GetAllGpuTemperaturesAsync()).GetHighestTemperature();

        // Assert
        highest.Value.Celsius.Should().Be(72m);
    }
}
