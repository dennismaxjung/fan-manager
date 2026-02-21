using Microsoft.Extensions.Configuration;

namespace FanManager.Core.Options;

/// <summary>
/// IPMI configuration
/// </summary>

public sealed record IpmiOptions
{
    [ConfigurationKeyName("HOST")]
    public string Host { get; init; } = "local";

    [ConfigurationKeyName("USERNAME")]
    public string Username { get; init; } = "root";

    [ConfigurationKeyName("PASSWORD")]
    public string Password { get; init; } = "calvin";
}
