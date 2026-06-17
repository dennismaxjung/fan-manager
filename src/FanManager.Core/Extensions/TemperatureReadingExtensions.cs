using FanManager.Core.Models;

namespace FanManager.Core.Extensions;

public static class TemperatureReadingExtensions
{
    extension<T>(IReadOnlyDictionary<T, TemperatureReading> temperatures)
    {
        public KeyValuePair<T, TemperatureReading> GetHighestTemperature()
        {
            if (!temperatures.Any())
                throw new InvalidOperationException("No temperatures available");

            var highest = temperatures.MaxBy(t => t.Value.Celsius);

            return highest;
        }
    }


    extension(IReadOnlyList<TemperatureReading> temperatures)
    {
        public TemperatureReading GetHighestTemperature()
        {
            if (!temperatures.Any())
                throw new InvalidOperationException("No temperatures available");

            var highest = temperatures.MaxBy(t => t.Celsius);

            return highest;
        }
    }
}
