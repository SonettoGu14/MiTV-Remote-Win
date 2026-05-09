using System.Runtime.InteropServices;

namespace MiTVRemote.Native;

/// <summary>
/// Win32 P/Invoke 声明集合。
/// 所有原生系统调用统一放在此处。
/// </summary>
internal static partial class NativeMethods
{
    // —— 键盘钩子 ——
    // 用于在遥控面板打开时，将键盘方向键/Enter/Backspace 映射为遥控器按键。
    // 参照原 Swift 项目 main.swift 中的 NSEvent.addLocalMonitorForEvents。

    public const int WH_KEYBOARD_LL = 13;

    [DllImport("user32.dll")]
    public static extern nint SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    public static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    public static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    public delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

    // —— 显示器亮度 (DDC/CI) ——
    // 用于通过 dxva2.dll 控制外接显示器亮度。
    // 参照原 Swift 项目 BrightnessController.swift。

    public const int MC_VCP_CODE_BRIGHTNESS = 0x10;

    [DllImport("dxva2.dll", EntryPoint = "GetNumberOfPhysicalMonitorsFromHMONITOR")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(nint hMonitor, out uint pdwNumberOfPhysicalMonitors);

    [DllImport("dxva2.dll", EntryPoint = "GetPhysicalMonitorsFromHMONITOR")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetPhysicalMonitorsFromHMONITOR(nint hMonitor, uint dwPhysicalMonitorArraySize,
        [Out] PhysicalMonitor[] pPhysicalMonitorArray);

    [DllImport("dxva2.dll", EntryPoint = "GetVCPFeatureAndVCPFeatureReply")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetVCPFeatureAndVCPFeatureReply(nint hMonitor, byte bVCPCode,
        nint pvct, out uint pdwCurrentValue, out uint pdwMaximumValue);

    [DllImport("dxva2.dll", EntryPoint = "SetVCPFeature")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetVCPFeature(nint hMonitor, byte bVCPCode, uint dwNewValue);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct PhysicalMonitor
    {
        public nint hPhysicalMonitor;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szPhysicalMonitorDescription;
    }
}
