using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using MiTVRemote.Models;

namespace MiTVRemote.Services;

/// <summary>
/// 局域网 MiTV 设备发现服务。
/// 枚举本机网卡，对每个 /24 子网并发探测 1-254。
/// 参照原 Swift 项目 MiTVController.swift 中的 LAN 扫描逻辑。
/// </summary>
public class DeviceDiscoveryService
{
    private readonly IMiTvService _miTvService;

    public DeviceDiscoveryService(IMiTvService miTvService)
    {
        _miTvService = miTvService;
    }

    public async Task<List<MiTvDevice>> DiscoverAsync(CancellationToken ct = default)
    {
        var results = new List<MiTvDevice>();
        var subnets = GetLocalSubnets();
        if (subnets.Count == 0)
            return results;

        var semaphore = new SemaphoreSlim(64);
        var tasks = new List<Task>();

        foreach (var subnet in subnets)
        {
            for (int i = 1; i <= 254; i++)
            {
                var ip = $"{subnet}.{i}";
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        cts.CancelAfter(450);
                        if (await _miTvService.IsAliveAsync(ip, cts.Token))
                        {
                            var info = await _miTvService.GetSystemInfoAsync(ip, ct);
                            var name = "MiTV";
                            lock (results)
                            {
                                results.Add(new MiTvDevice(name, ip));
                            }
                        }
                    }
                    catch
                    {
                        // 连接失败或超时，忽略
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, ct));
            }
        }

        await Task.WhenAll(tasks);
        return results;
    }

    /// <summary>
    /// 获取本机所有活动 IPv4 网卡的 /24 子网前缀。
    /// 参照原 Swift 项目中的 getifaddrs() 逻辑。
    /// </summary>
    private static List<string> GetLocalSubnets()
    {
        var subnets = new HashSet<string>();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up) continue;
            if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

            foreach (var addr in nic.GetIPProperties().UnicastAddresses)
            {
                if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                var ip = addr.Address.ToString();
                var lastDot = ip.LastIndexOf('.');
                if (lastDot > 0)
                {
                    subnets.Add(ip[..lastDot]);
                }
            }
        }
        return subnets.ToList();
    }
}
