namespace FanManager.Core.Interfaces;

/// <summary>
/// Defines a service for executing specific system commands asynchronously.
/// </summary>
public interface IProcessService
{
    /// <summary>
    /// Represents a set of supported commands that can be executed by the implementation
    /// of the <see cref="IProcessService"/> interface.
    /// </summary>
    public enum Command
    {
        /// <summary>
        /// Represents the Nvidia System Management Interface (nvidia-smi) command.
        /// This command is used for querying and managing the state of NVIDIA GPUs.
        /// </summary>
        NvidiaSmi,

        /// <summary>
        /// Represents the "IpmiTool" command, typically used for interfacing
        /// with Intelligent Platform Management Interface (IPMI) systems.
        /// This command is mapped to the "/usr/bin/ipmitool" executable.
        /// </summary>
        IpmiTool
    }

    /// <summary>
    /// Executes a specified command asynchronously with the provided arguments and cancellation token.
    /// </summary>
    /// <param name="command">
    /// The command to execute, represented by the <see cref="IProcessService.Command"/> enum.
    /// </param>
    /// <param name="arguments">
    /// The arguments to pass to the command.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous operation.
    /// The task result contains the standard output of the executed command as a string.
    /// </returns>
    public Task<string> ExecuteCommandAsync(
        Command command,
        string arguments,
        CancellationToken cancellationToken);
}
