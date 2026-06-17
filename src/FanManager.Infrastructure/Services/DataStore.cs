using FanManager.Core.Interfaces;
using FanManager.Core.Models;

namespace FanManager.Infrastructure.Services;

public class DataStore : IDataStore
{
    private readonly INvidiaSmiService _nvidiaSmiService;
    private readonly Task _initTask;

    private IReadOnlyList<GpuInfo> _gpuInfos = new List<GpuInfo>();

    public DataStore(INvidiaSmiService nvidiaSmiService)
    {
        _nvidiaSmiService = nvidiaSmiService;
        _initTask = InitializeAsync();
    }

    private async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _gpuInfos = await _nvidiaSmiService.GetGpuInfoAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GpuInfo>> GetGpuInfosAsync(CancellationToken cancellationToken)
    {
        await _initTask.WaitAsync(cancellationToken);
        return _gpuInfos;
    }
}
