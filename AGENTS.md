# AGENTS.md — Windows 版小米电视遥控器

## 项目概述

Windows 系统托盘遥控器，通过局域网 HTTP API（端口 6095）控制小米/红米电视和显示器。
原 macOS 版 [MiTV-Remote](https://github.com/SonettoGu14/MiTV-Remote) 的 Windows 移植。
本地 macOS 版参考路径：`/Users/gyk/GitHubProjects/MiTV-Remote`。

## 技术栈

- C# 12, .NET 8.0, WPF + System.Windows.Forms (NotifyIcon)
- 零外部 NuGet 包，纯 .NET BCL + P/Invoke
- MVVM 手动实现（手写 INotifyPropertyChanged，无框架）
- 配置持久化：`Microsoft.Win32.Registry` (HKCU\Software\MiTVRemote)
- 本地化：`.resx` 资源文件（已定义但 UI 尚未接入，目前仍硬编码中文字符串）

## 常用命令

```powershell
dotnet restore
dotnet build
dotnet run
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
dotnet format style --verify-no-changes   # 格式检查
dotnet format style                       # 格式自动修复
```

## 项目结构

```
MiTV-Remote-Win/
├── MiTV-Remote-Win.sln
└── MiTVRemote/
    ├── MiTVRemote.csproj          # net8.0-windows, win-x64, UseWPF+UseWindowsForms
    ├── App.xaml / App.xaml.cs     # 应用入口（ShutdownMode=OnExplicitShutdown，托盘模式）
    ├── AppConfig.cs               # 配置读写（环境变量 + Registry 回退）
    ├── Models/                    # 纯数据模型（record 类型，无逻辑）
    │   ├── MiTvDevice.cs          # 设备（Name + Host）
    │   ├── VolumeStatus.cs        # 音量状态（数值 + Percent 计算）
    │   ├── KeyCode.cs             # 遥控器按键码常量（up/down/left/right/enter/home/back/menu/power/volumeup/volumedown）
    │   └── DeviceSystemInfo.cs    # 设备系统信息（MAC 地址 → SigningMac 用于 MD5 签名）
    ├── Services/                  # 业务逻辑，无 UI 依赖
    │   ├── IMiTvService.cs        # 服务接口
    │   ├── MiTvHttpService.cs     # HTTP API 客户端（核心，实现所有 API 调用 + MD5 签名）
    │   ├── DeviceDiscoveryService.cs  # 局域网 /24 子网并发扫描（SemaphoreSlim(64), 超时 450ms）
    │   └── BrightnessService.cs   # 显示器亮度控制（骨架，一期不做）
    ├── UI/                        # WPF 视图
    │   ├── TrayIcon.cs            # NotifyIcon 管理 + 右键菜单
    │   ├── ControlPanel.xaml/.cs   # 遥控面板主窗口（弹出式 ToolWindow）
    │   ├── VolumeSlider.xaml/.cs   # 自定义音量滑块（占位，实际音量用 ControlPanel 内的 Slider）
    │   └── Styles.xaml            # 全局样式
    ├── Native/
    │   └── NativeMethods.cs       # P/Invoke 声明（键盘钩子、DDC/CI）
    ├── Localization/
    │   └── Strings.resx           # 英文字符串资源（UI 尚未接入）
    └── Resources/
        └── app.ico                # 托盘图标
```

## 架构

```
App.xaml.cs
  └─ 创建 MiTvHttpService + DeviceDiscoveryService
  └─ 创建 TrayIcon 并显示

TrayIcon (System.Windows.Forms.NotifyIcon)
  └─ 左键 / 右键菜单 → ShowControlPanel()
  └─ 创建 ControlPanel Window（ToolWindow, Topmost, 不显示任务栏）

ControlPanel
  └─ 持有 IMiTvService + DeviceDiscoveryService
  └─ 音量 Slider → IMiTvService.SetVolumeAsync(host, vol, signingMac)
  └─ D-pad / 功能键 → IMiTvService.SendKeyAsync(host, keyCode)
  └─ HDMI 切换 → IMiTvService.ChangeSourceAsync(host, sourceName)
  └─ 发现设备 → DeviceDiscoveryService.DiscoverAsync()
```

## 配置优先级

设备 IP 地址 (`AppConfig.MiTvHost`) 的读取优先级：
1. 环境变量 `TV_VOLUME_MITV_HOST`
2. 注册表 `HKEY_CURRENT_USER\Software\MiTVRemote\MiTVHost`
3. 默认值 `192.168.1.50`

## 核心 API 算法

### 音量设置（MD5 签名）

```
签名 = MD5("mitvsignsalt" + SigningMac(小写无冒号) + "setVolum" + 音量值 + Unix时间戳)
GET /general?action=setVolum&volum={v}&ts={ts}&sign={md5hex}
```

### 设备发现

- 枚举本机活动 IPv4 网卡，提取 /24 子网前缀
- 对每个子网 1-254 并发探测 `GET http://{ip}:6095/request?action=isalive`
- 并发限制：`SemaphoreSlim(64)`，超时 450ms

### 按键发送

```
GET /controller?action=keyevent&keycode={keycode}
```

KeyCode 常量：up, down, left, right, enter, home, back, menu, power, volumeup, volumedown

### HDMI 切换

```
GET /controller?action=changesource&source={Uri.EscapeDataString(sourceName)}
```

## 代码约定

- **命名空间**: `MiTVRemote`，子命名空间对应目录名（`MiTVRemote.Models`, `MiTVRemote.Services`, `MiTVRemote.UI`）
- **Model 类型**: 使用 `record`（值语义，适合数据载体）
- **WPF 事件**: `async void` 模式，内部 try/catch
- **UI 线程**: 通过 `Dispatcher.InvokeAsync` 回 UI 线程
- **资源清理**: 实现 `IDisposable` 的类在 `Dispose` 中释放
- **P/Invoke**: 统一放在 `NativeMethods.cs`（`partial class`），标记 `[DllImport]`
- **不需要注释**: 除非逻辑特别复杂或反直觉

## 已知限制和开发状态

- **键盘钩子**: `NativeMethods.cs` 已声明 `SetWindowsHookEx(WH_KEYBOARD_LL)`，但 ControlPanel 尚未实际安装钩子
- **托盘图标**: `TrayIcon.cs` 中图标文件加载被注释（TODO），运行时显示默认图标
- **音量滑块控件**: `VolumeSlider.xaml/.cs` 是占位骨架，实际音量通过 ControlPanel 内的 `Slider` 控制
- **BrightnessService**: 一期不做，所有方法抛出 `NotImplementedException`
- **本地化**: `Strings.resx` 已定义英文资源，但 UI 仍硬编码中文，未通过 ResourceManager 接入
- **没有测试**: 无单元测试或集成测试项目
- **没有 CI/CD**: 无 GitHub Actions 或其他 CI 配置
