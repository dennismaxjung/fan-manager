using FanManager.Core.Extensions;
using FanManager.Core.Models;

namespace FanManager.Tests.Extensions;

public class TemperatureReadingExtensionsTests
{
    [Fact]
    public void Dictionary_GetHighestTemperature_ReturnsEntryWithHighestCelsius()
    {
        var temperatures = new Dictionary<string, TemperatureReading>
            (StringComparer.OrdinalIgnoreCase)
        {
            ["cpu"] = new() { Celsius = 65 },
            ["gpu"] = new() { Celsius = 82 },
            ["disk"] = new() { Celsius = 45 },
        };

        var result = temperatures.GetHighestTemperature();

        result.Key.Should().Be("gpu");
        result.Value.Celsius.Should().Be(82);
    }

    [Fact]
    public void Dictionary_GetHighestTemperature_SingleEntry_ReturnsThatEntry()
    {
        var temperatures = new Dictionary<string, TemperatureReading>
            (StringComparer.OrdinalIgnoreCase)
        { ["only"] = new() { Celsius = 55 }, };

        var result = temperatures.GetHighestTemperature();

        result.Key.Should().Be("only");
        result.Value.Celsius.Should().Be(55);
    }

    [Fact]
    public void Dictionary_GetHighestTemperature_WithNegativeTemperatures_ReturnsLeastNegative()
    {
        var temperatures = new Dictionary<string, TemperatureReading>
            (StringComparer.OrdinalIgnoreCase)
        {
            ["a"] = new() { Celsius = -10 },
            ["b"] = new() { Celsius = -5 },
            ["c"] = new() { Celsius = -20 },
        };

        var result = temperatures.GetHighestTemperature();

        result.Key.Should().Be("b");
        result.Value.Celsius.Should().Be(-5);
    }

    [Fact]
    public void Dictionary_GetHighestTemperature_EmptyDictionary_ThrowsInvalidOperationException()
    {
        var temperatures = new Dictionary<string, TemperatureReading>(StringComparer.OrdinalIgnoreCase);

        Action act = () => temperatures.GetHighestTemperature();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No temperatures available");
    }

    [Fact]
    public void Dictionary_GetHighestTemperature_WorksWithIntegerKey()
    {
        var temperatures = new Dictionary<int, TemperatureReading>
        {
            [1] = new() { Celsius = 30 },
            [2] = new() { Celsius = 95 },
        };

        var result = temperatures.GetHighestTemperature();

        result.Key.Should().Be(2);
        result.Value.Celsius.Should().Be(95);
    }

    [Fact]
    public void List_GetHighestTemperature_ReturnsReadingWithHighestCelsius()
    {
        var temperatures = new List<TemperatureReading>
        {
            new() { Celsius = 65 }, new() { Celsius = 82 }, new() { Celsius = 45 },
        };

        var result = temperatures.GetHighestTemperature();

        result.Celsius.Should().Be(82);
    }

    [Fact]
    public void List_GetHighestTemperature_SingleEntry_ReturnsThatEntry()
    {
        var temperatures = new List<TemperatureReading> { new() { Celsius = 55 }, };

        var result = temperatures.GetHighestTemperature();

        result.Celsius.Should().Be(55);
    }

    [Fact]
    public void List_GetHighestTemperature_WithNegativeTemperatures_ReturnsLeastNegative()
    {
        var temperatures = new List<TemperatureReading>
        {
            new() { Celsius = -10 }, new() { Celsius = -5 }, new() { Celsius = -20 },
        };

        var result = temperatures.GetHighestTemperature();

        result.Celsius.Should().Be(-5);
    }

    [Fact]
    public void List_GetHighestTemperature_EmptyList_ThrowsInvalidOperationException()
    {
        var temperatures = new List<TemperatureReading>();

        Action act = () => temperatures.GetHighestTemperature();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No temperatures available");
    }

    [Fact]
    public void List_GetHighestTemperature_AllEqualTemperatures_ReturnsAnyEntry()
    {
        var temperatures = new List<TemperatureReading>
        {
            new() { Celsius = 50 }, new() { Celsius = 50 }, new() { Celsius = 50 },
        };

        var result = temperatures.GetHighestTemperature();

        result.Celsius.Should().Be(50);
    }
}
