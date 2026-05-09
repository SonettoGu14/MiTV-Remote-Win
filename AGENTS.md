# AGENTS.md — Windows 版小米电视遥控器

## 项目概述

Windows 原生系统托盘遥控器，通过局域网 HTTP API（端口 6095）控制小米/红米电视和显示器。功能包括音量调节、HDMI 输入源切换、D-pad 按键导航、设备自动发现。是原 macOS 版 [MiTV-Remote](https://github.com/anomalyco/MiTV-Remote) 的 Windows 移植。

## 技术栈

| 层级 | 技术 |
|------|------|
| 语言 | C# 12 |
| 运行时 | .NET 8.0 |
| UI 框架 | WPF (Windows Presentation Foundation) |
| 架构模式 | MVVM（轻量，不引入框架，手写 INotifyPropertyChanged） |
| HTTP | `System.Net.Http.HttpClient` |
| 加密 | `System.Security.Cryptography.MD5` |
| 网络枚举 | `System.Net.NetworkInformation.NetworkInterface` |
| 系统调用 | `P/Invoke` (`user32.dll`, `kernel32.dll`, `dxva2.dll`) |
| 配置存储 | `Properties.Settings` / `Microsoft.Win32.Registry` |
| 本地化 | `.resx` 资源文件 |
| 依赖 | 零外部 NuGet 包（纯 .NET BCL + P/Invoke） |

## 常用命令

```powershell
# 还原依赖
dotnet restore

# 构建（Debug）
dotnet build

# 构建（Release，单文件发布）
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# 代码格式化检查
dotnet format style --verify-no-changes

# 代码格式化自动修复
dotnet format style

# 运行
dotnet run
```

## 项目结构

```
MiTV-Remote-Win/
├── MiTV-Remote-Win.sln              # VS 解决方案
└── MiTVRemote/
    ├── MiTVRemote.csproj            # WPF 项目文件 (.NET 8, win-x64)
    ├── App.xaml / App.xaml.cs       # 应用入口（托盘模式，ShutdownMode=OnExplicitShutdown）
    ├── Models/                      # 纯数据模型（无逻辑）
    │   ├── MiTvDevice.cs            #   设备信息（名称 + IP）              ← 参照 MiTVDevice.swift
    │   ├── VolumeStatus.cs          #   音量状态（数值 + 百分比）          ← 参照 VolumeStatus.swift
    │   ├── KeyCode.cs               #   遥控器按键码枚举                   ← 参照 CECCommand.swift
    │   └── DeviceSystemInfo.cs      #   设备系统信息（MAC 地址用于签名）   ← 参照 MiTVSystemInfo.swift
    ├── Services/                    # 无 UI 的业务逻辑
    │   ├── IMiTvService.cs          #   服务接口
    │   ├── MiTvHttpService.cs       #   HTTP API 客户端（核心）           ← 参照 MiTVController.swift
    │   ├── DeviceDiscoveryService.cs #  局域网 /24 子网并发扫描           ← 参照 MiTVController.swift LAN 扫描
    │   └── BrightnessService.cs     #   显示器 DDC/CI 亮度控制（可选）     ← 参照 BrightnessController.swift
    ├── UI/                          # WPF 视图和控件
    │   ├── TrayIcon.cs              #   系统托盘图标管理（NotifyIcon）     ← 参照 main.swift AppDelegate
    │   ├── ControlPanel.xaml/.cs    #   弹出遥控面板主窗口
    │   ├── VolumeSlider.xaml/.cs    #   自定义音量滑块控件                ← 参照 VolumeSlider.swift
    │   └── Styles.xaml              #   全局样式/主题
    ├── Native/
    │   └── NativeMethods.cs         #   P/Invoke 声明（键盘钩子、DDC/CI、亮度等）
    ├── Localization/
    │   └── Strings.resx             #   英文字符串资源（后续添加 Strings.zh-CN.resx）
    └── Resources/
        └── app.ico                  #   托盘图标
```

## 架构

```
┌──────────────────────────────────────────────────────────┐
│                       App.xaml.cs                         │
│              启动托盘图标，持有服务实例                      │
├──────────────────────────────────────────────────────────┤
│                     TrayIcon.cs                           │
│  NotifyIcon → ContextMenu → 弹出 ControlPanel Window       │
│  键盘钩子 → 映射到遥控器按键                                 │
├────────────────────┬─────────────────────────────────────┤
│   ControlPanel      │  VolumeSlider（自定义控件）          │
│   (WPF Window)      │  D-pad / Home / Back / Menu 按钮    │
│                     │  HDMI 切换 / 设备发现 UI             │
├────────────────────┴─────────────────────────────────────┤
│                    Services                               │
│  ┌─────────────────────┐  ┌────────────────────────────┐ │
│  │ MiTvHttpService     │  │ DeviceDiscoveryService      │ │
│  │ - HTTP GET 到 :6095 │  │ - 枚举本机网卡               │ │
│  │ - 音量获取/设置      │  │ - 对 /24 子网并发探测       │ │
│  │ - 按键发送           │  │ - Task.WhenAll              │ │
│  │ - HDMI 切换          │  └────────────────────────────┘ │
│  │ - MD5 签名           │  ┌────────────────────────────┐ │
│  └─────────────────────┘  │ BrightnessService (可选)    │ │
│                           │ - DDC/CI (dxva2.dll)        │ │
│                           │ - SetMonitorBrightness      │ │
│                           └────────────────────────────┘ │
└──────────────────────────────────────────────────────────┘
```

## 核心算法（来自原 Swift 项目的参照）

### 1. 音量 API 签名（MD5）

参照 `MiTVController.swift` 中的 `setVolum` 方法：

```
签名 = MD5("mitvsignsalt" + MAC地址(无冒号,小写) + "setVolum" + 音量值 + 时间戳)
请求: GET /general?action=setVolum&volum={v}&ts={ts}&sign={md5hex}
```

### 2. 局域网设备发现

参照 `MiTVController.swift` 中的 `discoverMiTVDevices` 方法：

1. 通过 `NetworkInterface.GetAllNetworkInterfaces()` 枚举所有活动 IPv4 网卡
2. 对每个 /24 子网，使用 `Parallel.For` 或 `Task.WhenAll` 并发探测 1-254
3. 对每个 IP 发送 `GET http://{ip}:6095/request?action=isalive`
4. 超时 450ms，解析返回的 JSON 获取设备名

### 3. 按键码映射

参照 `MiTVController.swift` 中的 `sendKeyEvent` 方法：

| 按键 | keycode |
|------|---------|
| 上 | up |
| 下 | down |
| 左 | left |
| 右 | right |
| 确认 | enter |
| 主页 | home |
| 返回 | back |
| 菜单 | menu |
| 电源 | power |
| 音量+ | volumeup |
| 音量- | volumedown |

请求格式：`GET /controller?action=keyevent&keycode={keycode}`

### 4. HDMI 输入源切换

参照 `MiTVController.swift` 中的 `changeSource` 方法：

```
GET /controller?action=changesource&source={inputname}
```

`inputname` 值取决于电视返回的设备列表，常见的如 `"HDMI 1"`, `"HDMI 2"`。

## 编码约定

- **命名空间**: `MiTVRemote`，子命名空间对应目录名（如 `MiTVRemote.Models`, `MiTVRemote.Services`）
- **WPF 事件**: 使用 `async void` 模式，内部 try/catch 所有异常
- **UI 更新**: 确保通过 `Dispatcher.InvokeAsync` 回到 UI 线程
- **资源清理**: 所有 IDisposable 的子类在 `OnClosed`/`Dispose` 中释放
- **配置持久化**: 使用 `Properties.Settings`（默认），支持在运行时保存用户选择的设备 IP
- **本地化**: 所有用户可见字符串通过 `Strings.ResourceManager` 获取，不允许硬编码中文
- **P/Invoke**: 统一放在 `NativeMethods.cs`，使用 `partial` 类，标记 `[DllImport]`
- **不需要注释**: 除非特别复杂或反直觉的逻辑

## 关键平台差异（与 macOS 版对照）

| 功能 | macOS | Windows |
|------|-------|---------|
| 菜单栏图标 | `NSStatusBar.system.statusItem` | `System.Windows.Forms.NotifyIcon` |
| 弹出菜单 | `NSMenu` + `NSView` (custom view) | WPF `Window`（弹出式，无标题栏） |
| 键盘监听 | `NSEvent.addLocalMonitorForEvents` | `SetWindowsHookEx(WH_KEYBOARD_LL, ...)` |
| 网卡枚举 | `getifaddrs()` | `NetworkInterface.GetAllNetworkInterfaces()` |
| 音量 API 签名 | `CryptoKit.Insecure.MD5` | `System.Security.Cryptography.MD5` |
| 显示器 DDC/CI | `Process` 调用外部 `m1ddc` | Win32 `GetVCPFeatureAndVCPFeatureReply` |
| HDMI-CEC | `CoreRC.framework` (Apple 私有框架) | **一期不做**，未来可集成 libCEC |

## 一期不做的功能

- **HDMI-CEC**（需 libCEC + USB-CEC 硬件）
- **显示器亮度控制**（BrigthnessService 只搭建接口和骨架，不做完整实现）
- **多语言**（目前只有 Strings.resx 英文，中文资源文件后续添加）

## 注意事项

- 本项目强制使用 `win-x64` Runtime Identifier，不跨平台
- `NotifyIcon` 来自 `System.Windows.Forms`，需引用该程序集并在 WPF 中正确调度消息循环
- 局域网扫描使用原始 `HttpClient` + `Task.WhenAll`，注意 `SocketHttpHandler.MaxConnectionsPerServer` 限制
- 用户配置（上次连接的设备 IP）优先从环境变量 `TV_VOLUME_MITV_HOST` 读取，其次从 `Properties.Settings.MiTVHost` 读取
