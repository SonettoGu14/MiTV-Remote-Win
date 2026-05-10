# Windows 版小米电视遥控器

[![Platform](https://img.shields.io/badge/platform-Windows%2010%2B-blue)](https://dotnet.microsoft.com/)
[![Framework](https://img.shields.io/badge/framework-.NET%208.0-512BD4)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

在 Windows 系统托盘中运行的局域网遥控器，用于控制同一 Wi-Fi 网络下的小米/红米电视和显示器。

---

## 功能

- **音量控制** — 滑块拖拽 + 按钮微调
- **HDMI 切换** — 一键切换 HDMI 1 / HDMI 2 输入源
- **遥控导航** — 方向键（上/下/左/右）、确认、主页、返回、菜单、电源
- **键盘映射** — 遥控面板打开时，键盘方向键 / Enter / Backspace 自动映射为遥控器按键（开发中）
- **设备发现** — 自动扫描整个局域网找出所有 MiTV 设备
- **设备信息** — 实时显示已连接设备的 IP 和名称

## 系统要求

- Windows 10 1809+ 或 Windows 11
- [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- 电视/显示器与电脑在同一局域网

## 安装

从 [Releases](../../releases) 页面下载最新的 `MiTVRemote-v0.0.1-win-x64.zip`，解压后双击 `MiTVRemote.exe` 运行。

> **注意：** 需要先安装 [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)。

或者克隆仓库自行编译：

```powershell
git clone https://github.com/SonettoGu14/MiTV-Remote-Win.git
cd MiTV-Remote-Win
dotnet publish -c Release -r win-x64 --self-contained false
```

## 使用

1. 运行后，系统托盘会出现一个小米遥控器图标
2. 点击图标弹出遥控面板
3. 首次使用时点击「发现设备」按钮，自动扫描局域网
4. 从列表中选择你的电视即可开始遥控

默认会尝试连接 `192.168.1.50`，你也可以通过环境变量 `TV_VOLUME_MITV_HOST` 预置目标 IP。

## 相关项目

- [MiTV-Remote（macOS 版）](https://github.com/SonettoGu14/MiTV-Remote) — 本项目的原始版本，macOS 菜单栏运行

## License

MIT
