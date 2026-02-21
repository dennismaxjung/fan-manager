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
                "--query-gpu=index,name,driver_version --format=csv,noheader",
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
                "--query-gpu=index,name,driver_version --format=csv,noheader",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                         0, Tesla T10, 580.126.16
                         1, Tesla T10, 580.126.16
                         """);

        var sut = CreateSut();

        // Act
        var gpus = await sut.GetGpuInfoAsync();

        // Assert
        gpus.Should().HaveCount(2);
        gpus[0].Index.Should().Be(0);
        gpus[0].Name.Should().Be("Tesla T10");
        gpus[0].DriverVersion.Should().Be("580.126.16");
        gpus[1].Index.Should().Be(1);
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
                "--query-gpu=temperature.gpu --format=csv,noheader,nounits",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                         38
                         45
                         """);

        var sut = CreateSut();

        // Act
        var temps = await sut.GetAllGpuTemperaturesAsync();

        // Assert
        temps.Should().HaveCount(2);
        temps.Select(t => t.Celsius).Should().BeEquivalentTo(new[] { 38m, 45m }, o => o.WithStrictOrdering());
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
                "--query-gpu=temperature.gpu --format=csv,noheader,nounits",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                          45
                          not-a-number
                          67
                          """);

        var sut = CreateSut();

        // Act
        var temps = await sut.GetAllGpuTemperaturesAsync();

        // Assert
        temps.Select(t => t.Celsius).Should().BeEquivalentTo(new[] { 45m, 67m }, o => o.WithStrictOrdering());
    }

    [Fact]
    public Task GetHighestGpuTemperatureAsync_WhenNoTemps_ThrowsAsync()
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
                "--query-gpu=temperature.gpu --format=csv,noheader,nounits",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("\n\n");

        var sut = CreateSut();

        // Act
        var act = () => sut.GetHighestGpuTemperatureAsync();

        // Assert
        return act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No GPU temperatures available");
    }

    [Fact]
    public async Task GetHighestGpuTemperatureAsync_ReturnsMaxTemperatureAsync()
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
                "--query-gpu=temperature.gpu --format=csv,noheader,nounits",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                         55
                         72
                         61
                         """);

        var sut = CreateSut();

        // Act
        var highest = await sut.GetHighestGpuTemperatureAsync();

        // Assert
        highest.Celsius.Should().Be(72m);
    }
}
