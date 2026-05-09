using System.Runtime.InteropServices;

namespace MiTVRemote;

/// <summary>
/// 应用配置管理（持久化到 Windows 注册表 HKCU）。
/// </summary>
public static class AppConfig
{
    /// <summary>
    /// 上次连接的设备 IP 地址。
    /// 优先读取环境变量 TV_VOLUME_MITV_HOST，其次读取注册表。
    /// </summary>
    public static string MiTvHost
    {
        get
        {
            var env = Environment.GetEnvironmentVariable("TV_VOLUME_MITV_HOST");
            if (!string.IsNullOrEmpty(env)) return env;

            try
            {
                return Microsoft.Win32.Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\MiTVRemote",
                    "MiTVHost",
                    "192.168.1.50") as string ?? "192.168.1.50";
            }
            catch
            {
                return "192.168.1.50";
            }
        }
        set
        {
            try
            {
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\MiTVRemote",
                    "MiTVHost",
                    value);
            }
            catch
            {
                // 写入注册表失败则静默忽略
            }
        }
    }
}
