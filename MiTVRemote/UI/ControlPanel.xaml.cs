using System.Windows;
using System.Windows.Controls.Primitives;
using MiTVRemote.Models;
using MiTVRemote.Services;

namespace MiTVRemote.UI;

/// <summary>
/// 遥控面板主窗口。
/// 参照原 Swift 项目 main.swift 中的 makeControlsView()。
/// </summary>
public partial class ControlPanel : Window
{
    private readonly IMiTvService _miTvService;
    private readonly DeviceDiscoveryService _discovery;

    private string _host = AppConfig.MiTvHost;

    private string? _signingMac;

    public ControlPanel(IMiTvService miTvService, DeviceDiscoveryService discovery)
    {
        InitializeComponent();
        _miTvService = miTvService;
        _discovery = discovery;

        DeviceInfoLabel.Text = $"设备: {_host}";
        _ = RefreshDeviceInfo();

        // —— 音量按钮 ——
        VolumeUpBtn.Click += (_, _) => _ = SendKey(KeyCode.VolumeUp);
        VolumeDownBtn.Click += (_, _) => _ = SendKey(KeyCode.VolumeDown);
        VolumeSlider.ValueChanged += async (_, _) =>
        {
            if (_signingMac == null) return;
            var vol = (int)VolumeSlider.Value;
            await _miTvService.SetVolumeAsync(_host, vol, _signingMac);
        };

        // —— D-pad ——
        UpBtn.Click += (_, _) => _ = SendKey(KeyCode.Up);
        DownBtn.Click += (_, _) => _ = SendKey(KeyCode.Down);
        LeftBtn.Click += (_, _) => _ = SendKey(KeyCode.Left);
        RightBtn.Click += (_, _) => _ = SendKey(KeyCode.Right);
        OkBtn.Click += (_, _) => _ = SendKey(KeyCode.Enter);

        // —— 功能键 ——
        HomeBtn.Click += (_, _) => _ = SendKey(KeyCode.Home);
        BackBtn.Click += (_, _) => _ = SendKey(KeyCode.Back);
        MenuBtn.Click += (_, _) => _ = SendKey(KeyCode.Menu);
        PowerBtn.Click += (_, _) => _ = SendKey(KeyCode.Power);

        // —— HDMI ——
        Hdmi1Btn.Click += (_, _) => _ = ChangeSource("HDMI 1");
        Hdmi2Btn.Click += (_, _) => _ = ChangeSource("HDMI 2");

        // —— 发现设备 ——
        DiscoverBtn.Click += async (_, _) =>
        {
            DiscoverBtn.IsEnabled = false;
            DiscoverBtn.Content = "扫描中...";
            var devices = await _discovery.DiscoverAsync();
            if (devices.Count > 0)
            {
                var d = devices[0];
                _host = d.Host;
                AppConfig.MiTvHost = d.Host;
                DeviceInfoLabel.Text = $"设备: {d.Name} ({d.Host})";
                await RefreshDeviceInfo();
            }
            else
            {
                DeviceInfoLabel.Text = "未发现设备";
            }
            DiscoverBtn.IsEnabled = true;
            DiscoverBtn.Content = "发现设备";
        };
    }

    private async Task SendKey(string keyCode)
    {
        try
        {
            await _miTvService.SendKeyAsync(_host, keyCode);
        }
        catch (Exception ex)
        {
            Dispatcher.InvokeAsync(() =>
                DeviceInfoLabel.Text = $"错误: {ex.Message}");
        }
    }

    private async Task ChangeSource(string source)
    {
        try
        {
            await _miTvService.ChangeSourceAsync(_host, source);
        }
        catch (Exception ex)
        {
            Dispatcher.InvokeAsync(() =>
                DeviceInfoLabel.Text = $"错误: {ex.Message}");
        }
    }

    private async Task RefreshDeviceInfo()
    {
        try
        {
            var info = await _miTvService.GetSystemInfoAsync(_host);
            if (info != null)
            {
                _signingMac = info.SigningMac;
                DeviceInfoLabel.Text = $"设备: {_host}";
            }

            var vol = await _miTvService.GetVolumeAsync(_host);
            if (vol != null)
            {
                VolumeSlider.Value = vol.Percent;
                VolumeLabel.Text = $"{vol.Percent}%";
            }
        }
        catch
        {
            // 忽略加载错误
        }
    }
}
