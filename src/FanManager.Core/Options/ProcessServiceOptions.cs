using Microsoft.Extensions.Configuration;

namespace FanManager.Core.Options;

/// <summary>
/// ProcessService configuration options
/// </summary>
public sealed record ProcessServiceOptions
{
    [ConfigurationKeyName("IPMI_TOOL_PATH")]
    public string IpmiToolPath { get; init; } = "/usr/bin/ipmitool";

    [ConfigurationKeyName("NVIDIA_SMI_PATH")]
    public string NvidiaSmiPath { get; init; } = "/usr/local/bin/nvidia-smi";
}
