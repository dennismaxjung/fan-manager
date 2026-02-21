using FanManager.Core.Models;
using FanManager.Core.Options;
using FanManager.Core.Services;
using Microsoft.Extensions.Options;

namespace FanManager.Tests.Services;

public sealed class FanControlStrategyTests
{
    private readonly FanControlStrategy _sutWithoutMax;
    private readonly FanControlStrategy _sutWithMax;
    private readonly FanControlStrategy _sutWithLowMax;
    private readonly FanControlStrategy _sutWithMaxIsThreshold;

    private readonly IOptions<FanManagerOptions> _optionsWithoutMax;
    private readonly IOptions<FanManagerOptions> _optionsWithMax;
    private readonly IOptions<FanManagerOptions> _optionsWithLowMax;
    private readonly IOptions<FanManagerOptions> _optionsWithMaxIsThreshold;

    public FanControlStrategyTests()
    {
        _optionsWithoutMax = Options.Create(new FanManagerOptions
        {
            BaseFanSpeed = new(20),
            CpuTemperatureThreshold = 60,
            GpuTemperatureThreshold = 55
        });

        _optionsWithMax = Options.Create(new FanManagerOptions
        {
            BaseFanSpeed = new(20),
            CpuTemperatureThreshold = 60,
            GpuTemperatureThreshold = 55,
            GpuTemperatureMax = 80
        });

        _optionsWithLowMax = Options.Create(new FanManagerOptions
        {
            BaseFanSpeed = new(20),
            CpuTemperatureThreshold = 60,
            GpuTemperatureThreshold = 55,
            GpuTemperatureMax = 50
        });

        _optionsWithMaxIsThreshold = Options.Create(new FanManagerOptions
        {
            BaseFanSpeed = new(20),
            CpuTemperatureThreshold = 60,
            GpuTemperatureThreshold = 55,
            GpuTemperatureMax = 55
        });

        _sutWithoutMax = new FanControlStrategy(_optionsWithoutMax);
        _sutWithMax = new FanControlStrategy(_optionsWithMax);
        _sutWithLowMax = new FanControlStrategy(_optionsWithLowMax);
        _sutWithMaxIsThreshold = new FanControlStrategy(_optionsWithMaxIsThreshold);
    }

    #region WithoutMax

    [Fact]
    public void Calculate_WhenAllTemperaturesNormal_ReturnsManualControl_WithoutMax()
    {
        var sut = _sutWithoutMax;
        var options = _optionsWithoutMax;

        // Arrange
        var status = options.CreateSystemTemperatureStatus(45, 50);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.ManualControl>();
        var manualControl = (FanControlDecision.ManualControl)decision;
        manualControl.Speed.Value.Should().Be(options.Value.BaseFanSpeed.Value);
    }

    [Fact]
    public void Calculate_WhenCpuAboveThreshold_ReturnsDellControl_WithoutMax()
    {
        var sut = _sutWithoutMax;
        var options = _optionsWithoutMax;

        // Arrange
        var status = options.CreateSystemTemperatureStatus(65, 50);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.DellAutoControl>();
        var dellControl = (FanControlDecision.DellAutoControl)decision;
        dellControl.Reason.Should().Contain("CPU temperature");
    }

    [Fact]
    public void Calculate_WhenGpuAboveThreshold_ReturnsDynamicGpuControl_WithoutMax()
    {
        var sut = _sutWithoutMax;
        var options = _optionsWithoutMax;

        // Arrange
        var status = options.CreateSystemTemperatureStatus(50, 61);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.DynamicGpuControl>();
        var gpuControl = (FanControlDecision.DynamicGpuControl)decision;
        gpuControl.AdjustedSpeed.Value.Should().BeGreaterThan(options.Value.BaseFanSpeed.Value);
    }

    [Theory]
    [InlineData(55, 20)] // At threshold -> no increase
    [InlineData(58, 35)] // 3°C over -> +15%
    [InlineData(61, 50)] // 6°C over -> +30%
    [InlineData(64, 65)] // 9°C over -> +45%
    [InlineData(70, 80)] // 15°C over -> +60% (capped)
    public void Calculate_DynamicGpuControl_CalculatesCorrectFanSpeed_WithoutMax(
        decimal gpuTemp,
        int expectedFanSpeed)
    {
        var sut = _sutWithoutMax;
        var options = _optionsWithoutMax;

        // Arrange
        var status = options.CreateSystemTemperatureStatus(50, gpuTemp);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.DynamicGpuControl>();
        var gpuControl = (FanControlDecision.DynamicGpuControl)decision;
        gpuControl.AdjustedSpeed.Value.Should().Be(expectedFanSpeed);
    }

    [Fact]
    public void Calculate_WhenCpuAndGpuBothHigh_CpuTakesPriority_WithoutMax()
    {
        var sut = _sutWithoutMax;
        var options = _optionsWithoutMax;

        // Arrange - both above threshold
        var status = options.CreateSystemTemperatureStatus(65, 70);

        // Act
        var decision = sut.Calculate(status);

        // Assert - CPU safety has priority
        decision.Should().BeOfType<FanControlDecision.DellAutoControl>();
    }

    [Fact]
    public void Calculate_WithoutGpu_WorksCorrectly_WithoutMax()
    {
        var sut = _sutWithoutMax;
        var options = _optionsWithoutMax;

        // Arrange - no GPU
        var status = options.CreateSystemTemperatureStatus(50);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.ManualControl>();
    }

    [Fact]
    public void Calculate_MaxFanSpeedCappedAt100_WithoutMax()
    {
        var sut = _sutWithoutMax;
        var options = _optionsWithoutMax;

        // Arrange - extremely high GPU temp
        var status = options.CreateSystemTemperatureStatus(50, 120);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.DynamicGpuControl>();
        var gpuControl = (FanControlDecision.DynamicGpuControl)decision;
        gpuControl.AdjustedSpeed.Value.Should().BeLessThanOrEqualTo(100);
    }

    #endregion

    #region WithMax

    [Fact]
    public void Calculate_WhenAllTemperaturesNormal_ReturnsManualControl_WithMax()
    {
        var sut = _sutWithMax;
        var options = _optionsWithMax;

        // Arrange
        var status = options.CreateSystemTemperatureStatus(45, 50);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.ManualControl>();
        var manualControl = (FanControlDecision.ManualControl)decision;
        manualControl.Speed.Value.Should().Be(options.Value.BaseFanSpeed.Value);
    }

    [Fact]
    public void Calculate_WhenCpuAboveThreshold_ReturnsDellControl_WithMax()
    {
        var sut = _sutWithMax;
        var options = _optionsWithMax;

        // Arrange
        var status = options.CreateSystemTemperatureStatus(65, 50);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.DellAutoControl>();
        var dellControl = (FanControlDecision.DellAutoControl)decision;
        dellControl.Reason.Should().Contain("CPU temperature");
    }

    [Fact]
    public void Calculate_WhenGpuAboveThreshold_ReturnsDynamicGpuControl_WithMax()
    {
        var sut = _sutWithMax;
        var options = _optionsWithMax;

        // Arrange
        var status = options.CreateSystemTemperatureStatus(50, 61);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.DynamicGpuControl>();
        var gpuControl = (FanControlDecision.DynamicGpuControl)decision;
        gpuControl.AdjustedSpeed.Value.Should().BeGreaterThan(options.Value.BaseFanSpeed.Value);
    }

    [Theory]
    [InlineData(55, 20)] // Threshold -> base speed
    [InlineData(58, 30)]
    [InlineData(61, 39)]
    [InlineData(64, 49)]
    [InlineData(70, 68)]
    [InlineData(79, 97)]
    public void Calculate_DynamicGpuControl_CalculatesCorrectFanSpeed_WithMax(
        decimal gpuTemp,
        int expectedFanSpeed)
    {
        var sut = _sutWithMax;
        var options = _optionsWithMax;

        // Arrange
        var status = options.CreateSystemTemperatureStatus(50, gpuTemp);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.DynamicGpuControl>();
        var gpuControl = (FanControlDecision.DynamicGpuControl)decision;
        gpuControl.AdjustedSpeed.Value.Should().Be(expectedFanSpeed);
    }

    [Fact]
    public void Calculate_WhenCpuAndGpuBothHigh_CpuTakesPriority_WithMax()
    {
        var sut = _sutWithMax;
        var options = _optionsWithMax;

        // Arrange - both above threshold
        var status = options.CreateSystemTemperatureStatus(65, 70);

        // Act
        var decision = sut.Calculate(status);

        // Assert - CPU safety has priority
        decision.Should().BeOfType<FanControlDecision.DellAutoControl>();
    }

    [Fact]
    public void Calculate_WhenCpuAndGpuBothHighButGpuIsOverMax_GpuTakesPriority_WithMax()
    {
        var sut = _sutWithMax;
        var options = _optionsWithMax;

        // Arrange - both above threshold, GPU over max
        var status = options.CreateSystemTemperatureStatus(65, 85);

        // Act
        var decision = sut.Calculate(status);

        // Assert - GPU safety has priority
        decision.Should().BeOfType<FanControlDecision.MaxFanSpeed>();
    }

    [Fact]
    public void Calculate_GpuBetweenThresholdAndMax_UsesDynamicControl_WithMax()
    {
        var sut = _sutWithMax;
        var options = _optionsWithMax;

        // Arrange - GPU between threshold (55) and max (80)
        var status = options.CreateSystemTemperatureStatus(50, 65);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.DynamicGpuControl>();
        var gpuControl = (FanControlDecision.DynamicGpuControl)decision;
        gpuControl.AdjustedSpeed.Value.Should().BeInRange(21, 99); // Not 100, not base
    }

    [Fact]
    public void Calculate_WithoutGpu_WorksCorrectly_WithMax()
    {
        var sut = _sutWithMax;
        var options = _optionsWithMax;

        // Arrange - no GPU
        var status = options.CreateSystemTemperatureStatus(50);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.ManualControl>();
    }

    [Fact]
    public void Calculate_MaxFanSpeedCappedAt100_WithMax()
    {
        var sut = _sutWithMax;
        var options = _optionsWithMax;

        // Arrange - extremely high GPU temp
        var status = options.CreateSystemTemperatureStatus(50, 120);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.MaxFanSpeed>();
        var gpuControl = (FanControlDecision.MaxFanSpeed)decision;
        gpuControl.AdjustedSpeed.Value.Should().BeLessThanOrEqualTo(100);
    }

    #endregion

    #region WithLowMax

    [Fact]
    public void Calculate_WhenAllTemperaturesNormal_ReturnsManualControl_WithLowMax()
    {
        var sut = _sutWithLowMax;
        var options = _optionsWithLowMax;

        // Arrange
        var status = options.CreateSystemTemperatureStatus(45, 40);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.ManualControl>();
        var manualControl = (FanControlDecision.ManualControl)decision;
        manualControl.Speed.Value.Should().Be(options.Value.BaseFanSpeed.Value);
    }

    [Fact]
    public void Calculate_WhenCpuAboveThreshold_ReturnsDellControl_WithLowMax()
    {
        var sut = _sutWithLowMax;
        var options = _optionsWithLowMax;

        // Arrange
        var status = options.CreateSystemTemperatureStatus(65, 40);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.DellAutoControl>();
        var dellControl = (FanControlDecision.DellAutoControl)decision;
        dellControl.Reason.Should().Contain("CPU temperature");
    }

    [Fact]
    public void Calculate_WhenGpuAboveThreshold_ReturnsMaxFanSpeed_WithLowMax()
    {
        var sut = _sutWithLowMax;
        var options = _optionsWithLowMax;

        // Arrange - GPU above threshold (55) which is also above max (50)
        var status = options.CreateSystemTemperatureStatus(50, 61);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.MaxFanSpeed>();
        var gpuControl = (FanControlDecision.MaxFanSpeed)decision;
        gpuControl.AdjustedSpeed.Value.Should().Be(100);
    }

    [Theory]
    [InlineData(50, 100)] // At max -> 100%
    [InlineData(55, 100)] // At threshold -> 100%
    [InlineData(58, 100)]
    [InlineData(61, 100)]
    [InlineData(64, 100)]
    [InlineData(70, 100)]
    [InlineData(79, 100)]
    public void Calculate_MaxFanSpeed_CalculatesCorrectFanSpeed_WithLowMax(
        decimal gpuTemp,
        int expectedFanSpeed)
    {
        var sut = _sutWithLowMax;
        var options = _optionsWithLowMax;

        // Arrange
        var status = options.CreateSystemTemperatureStatus(50, gpuTemp);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.MaxFanSpeed>();
        var gpuControl = (FanControlDecision.MaxFanSpeed)decision;
        gpuControl.AdjustedSpeed.Value.Should().Be(expectedFanSpeed);
    }

    [Fact]
    public void Calculate_WhenCpuHighAndGpuBelowMax_CpuTakesPriority_WithLowMax()
    {
        var sut = _sutWithLowMax;
        var options = _optionsWithLowMax;

        // Arrange - CPU above the threshold (65 > 60), GPU below max (40 < 50)
        var status = options.CreateSystemTemperatureStatus(65, 40);

        // Act
        var decision = sut.Calculate(status);

        // Assert - CPU safety has priority
        decision.Should().BeOfType<FanControlDecision.DellAutoControl>();
    }

    [Fact]
    public void Calculate_WhenCpuAndGpuBothHigh_CpuTakesPriority_WithLowMax()
    {
        var sut = _sutWithLowMax;
        var options = _optionsWithLowMax;

        // Arrange - both above threshold, GPU also above max (56 > 50)
        var status = options.CreateSystemTemperatureStatus(65, 56);

        // Act
        var decision = sut.Calculate(status);

        // Assert - GPU max takes priority over the CPU threshold
        decision.Should().BeOfType<FanControlDecision.MaxFanSpeed>();
    }

    [Fact]
    public void Calculate_WhenCpuAndGpuBothHighButGpuIsOverMax_GpuTakesPriority_WithLowMax()
    {
        var sut = _sutWithLowMax;
        var options = _optionsWithLowMax;

        // Arrange - both above threshold, GPU far over max
        var status = options.CreateSystemTemperatureStatus(65, 85);

        // Act
        var decision = sut.Calculate(status);

        // Assert - GPU safety has priority
        decision.Should().BeOfType<FanControlDecision.MaxFanSpeed>();
    }

    [Fact]
    public void Calculate_WithoutGpu_WorksCorrectly_WithLowMax()
    {
        var sut = _sutWithLowMax;
        var options = _optionsWithLowMax;

        // Arrange - no GPU
        var status = options.CreateSystemTemperatureStatus(50);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.ManualControl>();
    }

    [Fact]
    public void Calculate_MaxFanSpeedCappedAt100_WithLowMax()
    {
        var sut = _sutWithLowMax;
        var options = _optionsWithLowMax;

        // Arrange - extremely high GPU temp
        var status = options.CreateSystemTemperatureStatus(50, 120);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.MaxFanSpeed>();
        var gpuControl = (FanControlDecision.MaxFanSpeed)decision;
        gpuControl.AdjustedSpeed.Value.Should().BeLessThanOrEqualTo(100);
    }

    #endregion

    #region WithMaxIsThreshold

    [Fact]
    public void Calculate_WhenAllTemperaturesNormal_ReturnsManualControl_WithMaxIsThreshold()
    {
        var sut = _sutWithMaxIsThreshold;
        var options = _optionsWithMaxIsThreshold;

        // Arrange
        var status = options.CreateSystemTemperatureStatus(45, 50);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.ManualControl>();
        var manualControl = (FanControlDecision.ManualControl)decision;
        manualControl.Speed.Value.Should().Be(options.Value.BaseFanSpeed.Value);
    }

    [Fact]
    public void Calculate_WhenCpuAboveThreshold_ReturnsDellControl_WithMaxIsThreshold()
    {
        var sut = _sutWithMaxIsThreshold;
        var options = _optionsWithMaxIsThreshold;

        // Arrange
        var status = options.CreateSystemTemperatureStatus(65, 50);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.DellAutoControl>();
        var dellControl = (FanControlDecision.DellAutoControl)decision;
        dellControl.Reason.Should().Contain("CPU temperature");
    }

    [Fact]
    public void Calculate_WhenGpuAboveThreshold_ReturnsMaxFanSpeed_WithMaxIsThreshold()
    {
        var sut = _sutWithMaxIsThreshold;
        var options = _optionsWithMaxIsThreshold;

        // Arrange - GPU above the threshold (which equals max)
        var status = options.CreateSystemTemperatureStatus(50, 61);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.MaxFanSpeed>();
        var gpuControl = (FanControlDecision.MaxFanSpeed)decision;
        gpuControl.AdjustedSpeed.Value.Should().BeGreaterThan(options.Value.BaseFanSpeed.Value);
    }

    [Theory]
    [InlineData(55, 100)] // At threshold & max -> 100%
    [InlineData(58, 100)]
    [InlineData(61, 100)]
    [InlineData(64, 100)]
    [InlineData(70, 100)]
    [InlineData(79, 100)]
    public void Calculate_MaxFanSpeed_CalculatesCorrectFanSpeed_WithMaxIsThreshold(
        decimal gpuTemp,
        int expectedFanSpeed)
    {
        var sut = _sutWithMaxIsThreshold;
        var options = _optionsWithMaxIsThreshold;

        // Arrange
        var status = options.CreateSystemTemperatureStatus(50, gpuTemp);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.MaxFanSpeed>();
        var gpuControl = (FanControlDecision.MaxFanSpeed)decision;
        gpuControl.AdjustedSpeed.Value.Should().Be(expectedFanSpeed);
    }

    [Fact]
    public void Calculate_WhenCpuAndGpuBothHigh_GpuTakesPriority_WithMaxIsThreshold()
    {
        var sut = _sutWithMaxIsThreshold;
        var options = _optionsWithMaxIsThreshold;

        // Arrange - CPU above the threshold (65 > 60), GPU at threshold/max (55 >= 55)
        var status = options.CreateSystemTemperatureStatus(65, 55);

        // Act
        var decision = sut.Calculate(status);

        // Assert - GPU at max takes priority over CPU threshold
        decision.Should().BeOfType<FanControlDecision.MaxFanSpeed>();
    }

    [Fact]
    public void Calculate_WhenCpuAndGpuBothHighButGpuIsOverMax_GpuTakesPriority_WithMaxIsThreshold()
    {
        var sut = _sutWithMaxIsThreshold;
        var options = _optionsWithMaxIsThreshold;

        // Arrange - both above threshold, GPU over max
        var status = options.CreateSystemTemperatureStatus(65, 85);

        // Act
        var decision = sut.Calculate(status);

        // Assert - GPU safety has priority
        decision.Should().BeOfType<FanControlDecision.MaxFanSpeed>();
    }

    [Fact]
    public void Calculate_WithoutGpu_WorksCorrectly_WithMaxIsThreshold()
    {
        var sut = _sutWithMaxIsThreshold;
        var options = _optionsWithMaxIsThreshold;

        // Arrange - no GPU
        var status = options.CreateSystemTemperatureStatus(50);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.ManualControl>();
    }

    [Fact]
    public void Calculate_MaxFanSpeedCappedAt100_WithMaxIsThreshold()
    {
        var sut = _sutWithMaxIsThreshold;
        var options = _optionsWithMaxIsThreshold;

        // Arrange - extremely high GPU temp
        var status = options.CreateSystemTemperatureStatus(50, 120);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.MaxFanSpeed>();
        var gpuControl = (FanControlDecision.MaxFanSpeed)decision;
        gpuControl.AdjustedSpeed.Value.Should().BeLessThanOrEqualTo(100);
    }

    #endregion

    #region EdgeCases - Boundary Values

    [Theory]
    [InlineData(59.9)] // Just below the threshold
    [InlineData(60.0)] // Exactly at the threshold
    [InlineData(60.1)] // Just above the threshold
    public void Calculate_CpuAtExactThreshold_BehavesCorrectly(decimal cpuTemp)
    {
        var sut = _sutWithoutMax;
        var options = _optionsWithoutMax;

        // Arrange
        var status = options.CreateSystemTemperatureStatus(cpuTemp, 50);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        if (cpuTemp >= 60)
            decision.Should().BeOfType<FanControlDecision.DellAutoControl>();
        else
            decision.Should().BeOfType<FanControlDecision.ManualControl>();
    }

    [Theory]
    [InlineData(54.9)] // Just below the threshold
    [InlineData(55.0)] // Exactly at the threshold
    [InlineData(55.1)] // Just above the threshold
    public void Calculate_GpuAtExactThreshold_BehavesCorrectly_WithoutMax(decimal gpuTemp)
    {
        var sut = _sutWithoutMax;
        var options = _optionsWithoutMax;

        // Arrange
        var status = options.CreateSystemTemperatureStatus(50, gpuTemp);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        if (gpuTemp >= 55)
            decision.Should().BeOfType<FanControlDecision.DynamicGpuControl>();
        else
            decision.Should().BeOfType<FanControlDecision.ManualControl>();
    }

    [Theory]
    [InlineData(79.9)] // Just below max
    [InlineData(80.0)] // Exactly at max
    [InlineData(80.1)] // Just above max
    public void Calculate_GpuAtExactMax_BehavesCorrectly_WithMax(decimal gpuTemp)
    {
        var sut = _sutWithMax;
        var options = _optionsWithMax;

        // Arrange
        var status = options.CreateSystemTemperatureStatus(50, gpuTemp);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        if (gpuTemp >= 80)
            decision.Should().BeOfType<FanControlDecision.MaxFanSpeed>();
        else
            decision.Should().BeOfType<FanControlDecision.DynamicGpuControl>();
    }

    [Fact]
    public void Calculate_GpuAtExactThresholdAndMax_ReturnsMaxFanSpeed_WithMaxIsThreshold()
    {
        var sut = _sutWithMaxIsThreshold;
        var options = _optionsWithMaxIsThreshold;

        // Arrange - GPU exactly at the threshold (which equals max: 55)
        var status = options.CreateSystemTemperatureStatus(50, 55);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.MaxFanSpeed>();
    }

    [Fact]
    public void Calculate_GpuAtExactMax_ReturnsMaxFanSpeed_WithLowMax()
    {
        var sut = _sutWithLowMax;
        var options = _optionsWithLowMax;

        // Arrange - GPU exactly at max (50), below threshold (55)
        var status = options.CreateSystemTemperatureStatus(50, 50);

        // Act
        var decision = sut.Calculate(status);

        // Assert
        decision.Should().BeOfType<FanControlDecision.MaxFanSpeed>();
    }

    #endregion
}
