using System.Diagnostics;
using FanManager.Core.Interfaces;
using FanManager.Core.Options;
using Microsoft.Extensions.Options;

namespace FanManager.Infrastructure.Services;

public class ProcessService : IProcessService
{
    private readonly ProcessServiceOptions _processServiceOptions;

    public ProcessService(IOptions<ProcessServiceOptions> processServiceOptions)
    {
        _processServiceOptions = processServiceOptions.Value;
    }

    private string MapCommand(IProcessService.Command command) => command switch
    {
        IProcessService.Command.IpmiTool => _processServiceOptions.IpmiToolPath,
        IProcessService.Command.NvidiaSmi => _processServiceOptions.NvidiaSmiPath,
    };

    public Task<string> ExecuteCommandAsync(IProcessService.Command command, string arguments, CancellationToken cancellationToken)
        => ExecuteAsync(MapCommand(command), arguments, cancellationToken);

    private async Task<string> ExecuteAsync(
        string command,
        string arguments,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Command '{command} {arguments}' failed with exit code {process.ExitCode}: {error}");
        }

        return output;
    }
}
