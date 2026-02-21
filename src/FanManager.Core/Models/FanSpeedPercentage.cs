namespace FanManager.Core.Models;

/// <summary>
/// Fan speed percentage (0-100)
/// </summary>
public readonly record struct FanSpeedPercentage
{
    private readonly int _value;

    public FanSpeedPercentage(int percentage)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(percentage);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(percentage, 100);
        _value = percentage;
    }

    public int Value => _value;

    public static implicit operator int(FanSpeedPercentage speed) => speed.Value;
    public static implicit operator FanSpeedPercentage(int percentage) => new(percentage);

    public override string ToString() => $"{_value}%";
}
