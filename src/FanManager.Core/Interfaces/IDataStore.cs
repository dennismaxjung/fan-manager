using FanManager.Core.Models;

namespace FanManager.Core.Interfaces;

public interface IDataStore
{
    public Task<IReadOnlyList<GpuInfo>> GetGpuInfosAsync(CancellationToken cancellationToken);
}
