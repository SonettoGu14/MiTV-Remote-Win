namespace MiTVRemote.Models;

/// <summary>
/// MiTV 设备系统信息，主要用于获取 MAC 地址以计算音量设置 API 的 MD5 签名。
/// 参照原 Swift 项目 MiTVSystemInfo.swift。
/// </summary>
public record DeviceSystemInfo(string? WifiMac, string? EthernetMac)
{
    /// <summary>
    /// 用于 API 签名的 MAC 地址：小写、去掉冒号。
    /// </summary>
    public string SigningMac
    {
        get
        {
            var mac = WifiMac ?? EthernetMac ?? string.Empty;
            return mac.Replace(":", "").ToLowerInvariant();
        }
    }
}
