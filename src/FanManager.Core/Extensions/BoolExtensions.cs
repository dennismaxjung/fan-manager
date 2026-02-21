namespace FanManager.Core.Extensions;

public static class BoolExtensions
{
    extension(bool value)
    {
        public string ToEnabledDisabled() => value ? "ENABLED" : "DISABLED";

    }

    extension(bool? value)
    {
        public string ToEnabledDisabled() => value.HasValue ? value.Value.ToEnabledDisabled() : "N/A";

    }
}
