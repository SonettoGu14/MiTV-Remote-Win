using MiTVRemote.Models;

namespace MiTVRemote.Services;

/// <summary>
/// MiTV HTTP 通信服务接口。
/// </summary>
public interface IMiTvService
{
    Task<bool> IsAliveAsync(string host, CancellationToken ct = default);
    Task<VolumeStatus?> GetVolumeAsync(string host, CancellationToken ct = default);
    Task<bool> SetVolumeAsync(string host, int volume, string signingMac, CancellationToken ct = default);
    Task SendKeyAsync(string host, string keyCode, CancellationToken ct = default);
    Task<bool> ChangeSourceAsync(string host, string sourceName, CancellationToken ct = default);
    Task<DeviceSystemInfo?> GetSystemInfoAsync(string host, CancellationToken ct = default);
    Task<bool> IsReachableAsync(string host, CancellationToken ct = default);
}
