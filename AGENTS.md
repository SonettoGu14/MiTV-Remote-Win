# AGENTS.md — Windows 版小米电视遥控器

## 项目概述

Windows 系统托盘遥控器，通过局域网 HTTP API（端口 6095）控制小米/红米电视和显示器。
原 macOS 版 [MiTV-Remote](https://github.com/SonettoGu14/MiTV-Remote) 的 Windows 移植。
本地 macOS 版参考路径：`D:\GitHubProjects\MiTV-Remote`。

## 技术栈

- C# 12, .NET 8.0, WPF + System.Windows.Forms (NotifyIcon)
- 零外部 NuGet 包，纯 .NET BCL + P/Invoke
- MVVM 手动实现（手写 INotifyPropertyChanged，无框架）
- 配置持久化：`Microsoft.Win32.Registry` (HKCU\Software\MiTVRemote)
- 本地化：`.resx` 资源文件，通过 `L.S("key")` 访问（英文默认 + zh-Hans 中文）

## 常用命令

```powershell
dotnet build
dotnet run
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
dotnet format style --verify-no-changes   # 格式检查（无 .editorconfig，使用默认规则）
dotnet format style                       # 格式自动修复
```

没有测试项目，没有 CI/CD，没有 `.editorconfig`。验证靠 `dotnet build` + 手动运行。

## 项目结构

```
MiTV-Remote-Win/
├── MiTV-Remote-Win.sln
└── MiTVRemote/
    ├── MiTVRemote.csproj          # net8.0-windows, win-x64, UseWPF+UseWindowsForms
    ├── App.xaml / App.xaml.cs     # 应用入口（ShutdownMode=OnExplicitShutdown，单实例 Mutex）
    ├── AppConfig.cs               # 配置读写（环境变量 + Registry 回退，MiTvHost + DeviceName）
    ├── L.cs                       # 本地化辅助类（L.S("key") / L.S("key", args)）
    ├── Models/                    # 纯数据模型（record 类型，无逻辑）
    │   ├── MiTvDevice.cs          # 设备（Name + Host）
    │   ├── VolumeStatus.cs        # 音量状态（Volume + MaxVolume + Percent）
    │   ├── KeyCode.cs             # 遥控器按键码常量
    │   └── DeviceSystemInfo.cs    # 设备系统信息（MAC 地址 → SigningMac 用于 MD5 签名）
    ├── Services/                  # 业务逻辑，无 UI 依赖
    │   ├── IMiTvService.cs        # 服务接口
    │   ├── MiTvHttpService.cs     # HTTP API 客户端（核心，含 MD5 签名 + 音量回退）
    │   ├── DeviceDiscoveryService.cs  # 局域网 /24 子网并发扫描
    │   ├── KeyboardHookService.cs # 全局键盘钩子（方向键/Enter/Backspace → 遥控按键）
    │   ├── BrightnessService.cs   # 显示器亮度控制（DDC/CI via dxva2.dll）
    ├── UI/                        # WPF 视图
    │   ├── TrayIcon.cs            # NotifyIcon 管理 + 右键菜单 + 自动加载 app.ico
    │   ├── ControlPanel.xaml/.cs   # 遥控面板主窗口（含设备选择弹窗）
    │   ├── VolumeSlider.xaml/.cs   # 自定义音量滑块（占位，实际用 ControlPanel 内的 Slider）
    │   └── Styles.xaml            # 全局样式
    ├── Native/
    │   └── NativeMethods.cs       # P/Invoke 声明（键盘钩子、DDC/CI）
    ├── Localization/
    │   ├── Strings.resx           # 英文字符串资源（默认）
    │   └── Strings.zh-Hans.resx   # 中文字符串资源
    └── Resources/
        └── app.ico                # 托盘图标（Content，复制到输出目录）
```

## 架构

```
App.xaml.cs
  └─ Mutex 单实例检查（Global\MiTVRemote_SingleInstance）
  └─ 创建 MiTvHttpService + DeviceDiscoveryService
  └─ 创建 TrayIcon 并显示

TrayIcon (System.Windows.Forms.NotifyIcon)
  └─ 左键 / 右键菜单 → ShowControlPanel()
  └─ 创建 ControlPanel Window（ToolWindow, Topmost, 不显示任务栏）

ControlPanel
  └─ 持有 IMiTvService + DeviceDiscoveryService + KeyboardHookService
  └─ 音量 Slider（debounce 150ms）→ SetVolumeWithFallbackAsync（签名 API + 按键回退）
  └─ D-pad / 功能键 → SendKeyAsync
  └─ HDMI 切换 → ChangeSourceAsync
  └─ 发现设备 → DiscoverAsync → 多设备选择弹窗 → 保存 DeviceName 到注册表
  └─ Loaded → 安装键盘钩子 / Closing → 卸载
```

## 配置优先级

设备 IP 地址 (`AppConfig.MiTvHost`) 的读取优先级：
1. 环境变量 `TV_VOLUME_MITV_HOST`
2. 注册表 `HKEY_CURRENT_USER\Software\MiTVRemote\MiTVHost`
3. 默认值 `192.168.1.50`

设备名称 (`AppConfig.DeviceName`)：仅注册表 `...\DeviceName`。

## 核心 API 算法

### 音量设置（MD5 签名）

```
签名 = MD5("mitvsignsalt" + SigningMac(小写无冒号) + "setVolum" + 音量值 + Unix时间戳)
GET /general?action=setVolum&volum={v}&ts={ts}&sign={md5hex}
```

签名失败时回退：读取当前音量 → 计算差值 → 循环发送 volumeup/volumedown 按键（45ms 间隔）。

### 设备发现

- 枚举本机活动 IPv4 网卡，提取 /24 子网前缀
- 对每个子网 1-254 并发探测 `GET http://{ip}:6095/request?action=isalive`
- 并发限制：`SemaphoreSlim(64)`，超时 450ms
- 解析 `data.devicename` 或 `data.device` 获取设备名

### 按键发送

```
GET /controller?keycode={keycode}
```

KeyCode 常量：up, down, left, right, enter, home, back, menu, power, volumeup, volumedown

### 键盘映射

`WH_KEYBOARD_LL` 全局钩子，ControlPanel 打开时安装，关闭时卸载。
映射：↑↓←→ → 方向键，Enter → enter，Backspace → back。映射的按键被抑制（不传递给其他应用）。

## 代码约定

- **命名空间**: `MiTVRemote`，子命名空间对应目录名（`MiTVRemote.Models`, `MiTVRemote.Services`, `MiTVRemote.UI`）
- **Model 类型**: 使用 `record`（值语义，适合数据载体）
- **WPF 事件**: `async void` 模式，内部 try/catch
- **Fire-and-forget**: `_ = SendKey(...)` 模式（方法内部有 try/catch，安全）
- **UI 线程**: 通过 `Dispatcher.InvokeAsync` 回 UI 线程
- **资源清理**: 实现 `IDisposable` 的类在 `Dispose` 中释放
- **P/Invoke**: 统一放在 `NativeMethods.cs`（`partial class`），标记 `[DllImport]`
- **不需要注释**: 除非逻辑特别复杂或反直觉
- **无 MainWindow**: 应用纯托盘驱动，`ShutdownMode=OnExplicitShutdown`，退出靠 `Application.Exit()`
- **ImplicitUsings**: 启用但移除了 `System.Windows.Forms`（避免 WPF/WinForms 命名空间冲突）

## 已知限制和开发状态

- **音量滑块控件**: `VolumeSlider.xaml/.cs` 是占位骨架，实际音量通过 ControlPanel 内的 `Slider` 控制
- **没有测试**: 无单元测试或集成测试项目
- **没有 CI/CD**: 无 GitHub Actions 或其他 CI 配置
