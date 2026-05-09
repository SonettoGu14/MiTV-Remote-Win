using System.Windows.Forms;
using MiTVRemote.Services;

namespace MiTVRemote.UI;

/// <summary>
/// 系统托盘图标管理器。
/// 负责创建 NotifyIcon、弹出遥控面板、以及全局键盘钩子。
/// 参照原 Swift 项目 main.swift 中的 AppDelegate。
/// </summary>
public class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly IMiTvService _miTvService;
    private readonly DeviceDiscoveryService _discovery;
    private ControlPanel? _panel;

    public TrayIcon(IMiTvService miTvService, DeviceDiscoveryService discovery)
    {
        _miTvService = miTvService;
        _discovery = discovery;

        _notifyIcon = new NotifyIcon
        {
            Text = "小米电视遥控器",
            Visible = true
        };

        // TODO: 使用实际图标文件
        // _notifyIcon.Icon = new Icon("Resources/app.ico");

        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                ShowControlPanel();
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("打开遥控器", null, (_, _) => ShowControlPanel());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("退出", null, (_, _) => Application.Exit());
        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    public void Show()
    {
        // NotifyIcon 已在构造函数中设为 Visible
    }

    private void ShowControlPanel()
    {
        if (_panel?.IsVisible == true)
        {
            _panel.Activate();
            return;
        }

        _panel?.Close();
        _panel = new ControlPanel(_miTvService, _discovery);
        _panel.Show();
    }

    public void Dispose()
    {
        _panel?.Close();
        _panel = null;
        _notifyIcon.Dispose();
    }
}
