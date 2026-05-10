using System.Threading;
using System.Windows;
using MiTVRemote.Services;
using MiTVRemote.UI;

namespace MiTVRemote;

/// <summary>
/// Windows 版小米电视遥控器 —— 入口点。
/// 运行在系统托盘模式，不显示任务栏按钮，不显示主窗口（除非用户点击托盘图标弹出遥控面板）。
/// </summary>
public partial class App : Application
{
    private const string MutexName = "Global\\MiTVRemote_SingleInstance";
    private Mutex? _mutex;
    private bool _ownsMutex;
    private TrayIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, MutexName, out _ownsMutex);
        if (!_ownsMutex)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);

        var httpService = new MiTvHttpService();
        var discoveryService = new DeviceDiscoveryService(httpService);

        _trayIcon = new TrayIcon(httpService, discoveryService);
        _trayIcon.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        if (_ownsMutex) _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
