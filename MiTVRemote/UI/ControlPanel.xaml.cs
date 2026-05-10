using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MiTVRemote.Models;
using MiTVRemote.Services;

namespace MiTVRemote.UI;

public partial class ControlPanel : Window
{
    private readonly IMiTvService _miTvService;
    private readonly DeviceDiscoveryService _discovery;
    private readonly KeyboardHookService _keyboardHook;
    private readonly CancellationTokenSource _cts = new();

    private string _host = AppConfig.MiTvHost;
    private string? _signingMac;
    private CancellationTokenSource? _volumeDebounceCts;
    private bool _suppressVolumeChanged;

    public ControlPanel(IMiTvService miTvService, DeviceDiscoveryService discovery)
    {
        InitializeComponent();
        _miTvService = miTvService;
        _discovery = discovery;

        _keyboardHook = new KeyboardHookService(keyCode => _ = SendKey(keyCode));
        Loaded += (_, _) => _keyboardHook.Install();
        Closing += (_, _) => { _cts.Cancel(); _keyboardHook.Uninstall(); };

        DiscoverBtn.Content = L.S("DiscoverDevices");
        DeviceInfoLabel.Text = AppConfig.DeviceName != null
            ? L.S("DeviceInfoFormat", AppConfig.DeviceName, _host)
            : L.S("DeviceLabel", _host);
        VolumeSlider.IsEnabled = false;
        _ = RefreshDeviceInfo();

        // —— 音量按钮 ——
        VolumeUpBtn.Click += (_, _) => _ = SendKeyAndRefreshVolume(KeyCode.VolumeUp);
        VolumeDownBtn.Click += (_, _) => _ = SendKeyAndRefreshVolume(KeyCode.VolumeDown);
        VolumeSlider.ValueChanged += (_, _) =>
        {
            if (_suppressVolumeChanged) return;
            if (string.IsNullOrEmpty(_signingMac))
            {
                DeviceInfoLabel.Text = L.S("NoMacError");
                return;
            }
            var vol = (int)VolumeSlider.Value;
            VolumeLabel.Text = $"{vol}%";
            _volumeDebounceCts?.Cancel();
            _volumeDebounceCts = new CancellationTokenSource();
            var ct = _volumeDebounceCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(150, ct);
                    await _miTvService.SetVolumeWithFallbackAsync(_host, vol, _signingMac);
                    await RefreshVolume();
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Dispatcher.InvokeAsync(async () =>
                    {
                        try
                        {
                            DeviceInfoLabel.Text = L.S("ErrorFormat", ex.Message);
                            await RefreshVolume();
                        }
                        catch { }
                    });
                }
            });
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
            try
            {
                DiscoverBtn.IsEnabled = false;
                DiscoverBtn.Content = L.S("Scanning");
                var devices = await _discovery.DiscoverAsync(_cts.Token);
                if (devices.Count == 0)
                {
                    DeviceInfoLabel.Text = L.S("NoDevicesFound");
                }
                else
                {
                    var selected = devices.Count == 1 ? devices[0] : PickDevice(devices);
                    if (selected != null)
                    {
                        _host = selected.Host;
                        AppConfig.MiTvHost = selected.Host;
                        AppConfig.DeviceName = selected.Name;
                        DeviceInfoLabel.Text = L.S("DeviceInfoFormat", selected.Name, selected.Host);
                        await RefreshDeviceInfo();
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                DeviceInfoLabel.Text = L.S("ErrorFormat", ex.Message);
            }
            finally
            {
                DiscoverBtn.IsEnabled = true;
                DiscoverBtn.Content = L.S("DiscoverDevices");
            }
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
                DeviceInfoLabel.Text = L.S("ErrorFormat", ex.Message));
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
                DeviceInfoLabel.Text = L.S("ErrorFormat", ex.Message));
        }
    }

    private async Task SendKeyAndRefreshVolume(string keyCode)
    {
        try
        {
            await _miTvService.SendKeyAsync(_host, keyCode);
            await Task.Delay(300);
            await RefreshVolume();
        }
        catch (Exception ex)
        {
            Dispatcher.InvokeAsync(() =>
                DeviceInfoLabel.Text = L.S("ErrorFormat", ex.Message));
        }
    }

    private async Task RefreshVolume()
    {
        var vol = await _miTvService.GetVolumeAsync(_host);
        if (vol != null)
        {
            _suppressVolumeChanged = true;
            Dispatcher.Invoke(() =>
            {
                VolumeSlider.Maximum = vol.MaxVolume;
                VolumeSlider.Value = vol.Volume;
                VolumeLabel.Text = $"{vol.Percent}%";
            });
            _suppressVolumeChanged = false;
        }
    }

    private async Task RefreshDeviceInfo()
    {
        try
        {
            var info = await _miTvService.GetSystemInfoAsync(_host, _cts.Token);
            if (info != null)
            {
                _signingMac = info.SigningMac;
                if (string.IsNullOrEmpty(AppConfig.DeviceName))
                    DeviceInfoLabel.Text = L.S("DeviceLabel", _host);
            }

            var vol = await _miTvService.GetVolumeAsync(_host, _cts.Token);
            if (vol != null)
            {
                _suppressVolumeChanged = true;
                VolumeSlider.Maximum = vol.MaxVolume;
                VolumeSlider.Value = vol.Volume;
                _suppressVolumeChanged = false;
                VolumeSlider.IsEnabled = true;
                VolumeLabel.Text = $"{vol.Percent}%";
            }
        }
        catch (OperationCanceledException) { }
        catch
        {
            // Ignore load errors
        }
    }

    private static MiTvDevice? PickDevice(List<MiTvDevice> devices)
    {
        var dialog = new Window
        {
            Title = L.S("SelectDevice"),
            Width = 300,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Application.Current.Windows.OfType<ControlPanel>().FirstOrDefault(),
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false
        };

        var listBox = new ListBox { Margin = new Thickness(10) };
        foreach (var d in devices)
            listBox.Items.Add($"{d.Name} ({d.Host})");
        listBox.SelectedIndex = 0;

        var okButton = new Button
        {
            Content = L.S("Ok"),
            Width = 80,
            Margin = new Thickness(10),
            IsDefault = true
        };
        okButton.Click += (_, _) => dialog.DialogResult = true;

        var stack = new StackPanel();
        stack.Children.Add(listBox);
        stack.Children.Add(okButton);
        dialog.Content = stack;

        if (dialog.ShowDialog() == true && listBox.SelectedIndex >= 0)
            return devices[listBox.SelectedIndex];

        return null;
    }
}
