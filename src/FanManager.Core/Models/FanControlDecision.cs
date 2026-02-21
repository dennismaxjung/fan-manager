namespace FanManager.Core.Models;

/// <summary>
/// Fan control decision
/// </summary>
public abstract record FanControlDecision(DateTimeOffset Timestamp)
{
    public sealed record ManualControl(
        FanSpeedPercentage Speed,
        DateTimeOffset Timestamp) : FanControlDecision(Timestamp);

    public sealed record DellAutoControl(
        string Reason,
        DateTimeOffset Timestamp) : FanControlDecision(Timestamp);

    public sealed record DynamicGpuControl(
        FanSpeedPercentage AdjustedSpeed,
        string Reason,
        DateTimeOffset Timestamp) : FanControlDecision(Timestamp);

    public sealed record MaxFanSpeed(
        string Reason,
        DateTimeOffset Timestamp) : FanControlDecision(Timestamp)
    {
        public FanSpeedPercentage AdjustedSpeed => new(100);
    };
}
