using FanManager.Core.Interfaces;
using FanManager.Core.Models;
using FanManager.Core.Options;
using FanManager.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FanManager.Tests.Services;

public sealed class IpmiServiceTests
{
    private readonly Mock<IProcessService> _process = new();
    private readonly Mock<ILogger<IpmiService>> _logger = new();

    private static IOptions<IpmiOptions> LocalOptions()
        => Options.Create(new IpmiOptions
        {
            Host = "local",
            Username = "USER",
            Password = "PASSWORD"
        });

    private static IOptions<IpmiOptions> RemoteOptions()
        => Options.Create(new IpmiOptions
        {
            Host = "idrac.example.test",
            Username = "USER",
            Password = "PASSWORD"
        });

    private IpmiService CreateSut(IOptions<IpmiOptions> options)
        => new(_logger.Object, options, _process.Object);

    [Fact]
    public async Task IsAvailableAsync_WhenCommandSucceeds_ReturnsTrueAsync()
    {
        // Arrange
        var sut = CreateSut(LocalOptions());

        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.IpmiTool,
                "sdr type temperature",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                Inlet Temp       | 04h | ok  |  7.1 | 19 degrees C
                Exhaust Temp     | 01h | ok  |  7.1 | 29 degrees C
                Temp             | 0Eh | ok  |  3.1 | 34 degrees C
                Temp             | 0Fh | ok  |  3.2 | 32 degrees C
                """);

        // Act
        var available = await sut.IsAvailableAsync();

        // Assert
        available.Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailableAsync_WhenCommandThrows_ReturnsFalseAsync()
    {
        // Arrange
        var sut = CreateSut(LocalOptions());

        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.IpmiTool,
                "sdr type temperature",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("ipmitool failed"));

        // Act
        var available = await sut.IsAvailableAsync();

        // Assert
        available.Should().BeFalse();
    }

    [Fact]
    public async Task GetCpuTemperatureAsync_LocalHost_UsesPlainCommand_AndParsesHighestTempSensorReadingAsync()
    {
        // Arrange
        var sut = CreateSut(LocalOptions());

        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.IpmiTool,
                "sdr type temperature",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                Inlet Temp       | 04h | ok  |  7.1 | 19 degrees C
                Exhaust Temp     | 01h | ok  |  7.1 | 29 degrees C
                Temp             | 0Eh | ok  |  3.1 | 34 degrees C
                Temp             | 0Fh | ok  |  3.2 | 32 degrees C
                """);

        // Act
        var temp = await sut.GetCpuTemperatureAsync();

        // Assert
        temp.Celsius.Should().Be(34m);

        _process.Verify(p => p.ExecuteCommandAsync(
            IProcessService.Command.IpmiTool,
            "sdr type temperature",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCpuTemperatureAsync_RemoteHost_PrependsLanplusArgsAsync()
    {
        // Arrange
        var sut = CreateSut(RemoteOptions());

        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.IpmiTool,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                Inlet Temp       | 04h | ok  |  7.1 | 20 degrees C
                Exhaust Temp     | 01h | ok  |  7.1 | 30 degrees C
                Temp             | 0Eh | ok  |  3.1 | 55 degrees C
                Temp             | 0Fh | ok  |  3.2 | 52 degrees C
                """);

        // Act
        var temp = await sut.GetCpuTemperatureAsync();

        // Assert
        temp.Celsius.Should().Be(55m);

        _process.Verify(p => p.ExecuteCommandAsync(
            IProcessService.Command.IpmiTool,
            "-I lanplus -H idrac.example.test -U USER -P PASSWORD sdr type temperature",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public Task GetCpuTemperatureAsync_WhenOutputNotParseable_ThrowsAsync()
    {
        // Arrange
        var sut = CreateSut(LocalOptions());

        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.IpmiTool,
                "sdr type temperature",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                Inlet Temp       | 04h | ok  |  7.1 | 19 degrees C
                Exhaust Temp     | 01h | ok  |  7.1 | 29 degrees C
                (no generic Temp sensors found here)
                """);

        // Act
        var act = () => sut.GetCpuTemperatureAsync();

        // Assert
        return act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Could not read CPU temperature");
    }

    [Fact]
    public async Task EnableManualFanControlAsync_SendsExpectedRawCommandAsync()
    {
        // Arrange
        var sut = CreateSut(LocalOptions());

        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.IpmiTool,
                "raw 0x30 0x30 0x01 0x00",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        // Act
        await sut.EnableManualFanControlAsync();

        // Assert
        _process.Verify(p => p.ExecuteCommandAsync(
            IProcessService.Command.IpmiTool,
            "raw 0x30 0x30 0x01 0x00",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisableManualFanControlAsync_SendsExpectedRawCommandAsync()
    {
        // Arrange
        var sut = CreateSut(LocalOptions());

        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.IpmiTool,
                "raw 0x30 0x30 0x01 0x01",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        // Act
        await sut.DisableManualFanControlAsync();

        // Assert
        _process.Verify(p => p.ExecuteCommandAsync(
            IProcessService.Command.IpmiTool,
            "raw 0x30 0x30 0x01 0x01",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0, "0x00")]
    [InlineData(1, "0x01")]
    [InlineData(15, "0x0F")]
    [InlineData(16, "0x10")]
    [InlineData(100, "0x64")]
    public async Task SetFanSpeedAsync_FormatsSpeedAs2DigitHexAsync(int speed, string expectedHex)
    {
        // Arrange
        var sut = CreateSut(LocalOptions());

        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.IpmiTool,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        // Act
        await sut.SetFanSpeedAsync(new FanSpeedPercentage(speed));

        // Assert
        _process.Verify(p => p.ExecuteCommandAsync(
            IProcessService.Command.IpmiTool,
            $"raw 0x30 0x30 0x02 0xff {expectedHex}",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentDellThirdPartyCoolingBehaviour_WhenDisabled_ReturnsFalseAsync()
    {
        // Arrange
        var sut = CreateSut(LocalOptions());

        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.IpmiTool,
                "raw 0x30 0xce 0x01 0x16 0x05 0x00 0x00 0x00",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("16 05 00 00 00 05 00 01 00 00");

        // Act
        var result = await sut.GetCurrentDellThirdPartyCoolingBehaviourAsync();

        // Assert
        result.Should().BeFalse();

        _process.Verify(p => p.ExecuteCommandAsync(
            IProcessService.Command.IpmiTool,
            "raw 0x30 0xce 0x01 0x16 0x05 0x00 0x00 0x00",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public Task GetCurrentDellThirdPartyCoolingBehaviour_WhenOutputUnexpected_ThrowsAsync()
    {
        // Arrange
        var sut = CreateSut(LocalOptions());

        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.IpmiTool,
                "raw 0x30 0xce 0x01 0x16 0x05 0x00 0x00 0x00",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("unexpected output");

        // Act
        var act = () => sut.GetCurrentDellThirdPartyCoolingBehaviourAsync();

        // Assert
        return act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unexpected third-party PCIe card cooling response: unexpected output");
    }

    [Fact]
    public async Task SetDellThirdPartyCoolingBehaviour_WhenEnabled_SendsCorrectCommandAsync()
    {
        // Arrange
        var sut = CreateSut(LocalOptions());

        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.IpmiTool,
                "raw 0x30 0xce 0x00 0x16 0x05 0x00 0x00 0x00 0x05 0x00 0x00 0x00 0x00",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        // Act
        await sut.SetDellThirdPartyCoolingBehaviourAsync(true);

        // Assert
        _process.Verify(p => p.ExecuteCommandAsync(
            IProcessService.Command.IpmiTool,
            "raw 0x30 0xce 0x00 0x16 0x05 0x00 0x00 0x00 0x05 0x00 0x00 0x00 0x00",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetDellThirdPartyCoolingBehaviour_WhenDisabled_SendsCorrectCommandAsync()
    {
        // Arrange
        var sut = CreateSut(LocalOptions());

        _process
            .Setup(p => p.ExecuteCommandAsync(
                IProcessService.Command.IpmiTool,
                "raw 0x30 0xce 0x00 0x16 0x05 0x00 0x00 0x00 0x05 0x00 0x01 0x00 0x00",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        // Act
        await sut.SetDellThirdPartyCoolingBehaviourAsync(false);

        // Assert
        _process.Verify(p => p.ExecuteCommandAsync(
            IProcessService.Command.IpmiTool,
            "raw 0x30 0xce 0x00 0x16 0x05 0x00 0x00 0x00 0x05 0x00 0x01 0x00 0x00",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
