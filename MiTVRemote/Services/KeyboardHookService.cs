using System.Diagnostics;
using System.Runtime.InteropServices;
using MiTVRemote.Models;
using MiTVRemote.Native;

namespace MiTVRemote.Services;

/// <summary>
/// 全局键盘钩子服务。
/// 当遥控面板打开时，将方向键/Enter/Backspace 映射为遥控器按键。
/// 参照原 Swift 项目 main.swift 中的 NSEvent.addLocalMonitorForEvents + addGlobalMonitorForEvents。
/// </summary>
public class KeyboardHookService : IDisposable
{
    private readonly Action<string> _onRemoteKey;
    private nint _hookId;
    private NativeMethods.LowLevelKeyboardProc? _proc;

    public KeyboardHookService(Action<string> onRemoteKey)
    {
        _onRemoteKey = onRemoteKey;
    }

    public void Install()
    {
        if (_hookId != 0) return;
        _proc = HookCallback;
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;
        var hModule = NativeMethods.GetModuleHandle(module?.ModuleName ?? string.Empty);
        _hookId = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _proc, hModule, 0);
    }

    public void Uninstall()
    {
        if (_hookId == 0) return;
        NativeMethods.UnhookWindowsHookEx(_hookId);
        _hookId = 0;
    }

    public void Dispose()
    {
        Uninstall();
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode == NativeMethods.HC_ACTION &&
            wParam is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN)
        {
            var vkCode = Marshal.ReadByte(lParam);
            var keyCode = vkCode switch
            {
                NativeMethods.VK_UP => KeyCode.Up,
                NativeMethods.VK_DOWN => KeyCode.Down,
                NativeMethods.VK_LEFT => KeyCode.Left,
                NativeMethods.VK_RIGHT => KeyCode.Right,
                NativeMethods.VK_RETURN => KeyCode.Enter,
                NativeMethods.VK_BACK => KeyCode.Back,
                _ => null
            };

            if (keyCode != null)
            {
                _onRemoteKey(keyCode);
                return 1; // Suppress the key
            }
        }

        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
}
