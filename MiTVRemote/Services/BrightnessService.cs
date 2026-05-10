using System.Runtime.InteropServices;
using MiTVRemote.Native;

namespace MiTVRemote.Services;

/// <summary>
/// 显示器亮度控制服务（通过 DDC/CI）。
/// 参照原 Swift 项目 BrightnessController.swift。
/// </summary>
public class BrightnessService
{
    public Task<int?> GetBrightnessAsync(CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            int? result = null;
            NativeMethods.EnumDisplayMonitors(nint.Zero, nint.Zero, (hMonitor, _, _, _) =>
            {
                if (result != null) return true;

                if (!NativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out var count) || count == 0)
                    return true;

                var monitors = new NativeMethods.PhysicalMonitor[count];
                if (!NativeMethods.GetPhysicalMonitorsFromHMONITOR(hMonitor, count, monitors))
                    return true;

                for (int i = 0; i < count; i++)
                {
                    var h = monitors[i].hPhysicalMonitor;
                    try
                    {
                        if (result == null &&
                            NativeMethods.GetVCPFeatureAndVCPFeatureReply(
                                h, NativeMethods.MC_VCP_CODE_BRIGHTNESS, nint.Zero, out var current, out _))
                        {
                            result = (int)current;
                        }
                    }
                    finally
                    {
                        NativeMethods.DestroyPhysicalMonitor(h);
                    }
                }

                return true;
            }, nint.Zero);

            return result;
        }, ct);
    }

    public Task SetBrightnessAsync(int brightness, CancellationToken ct = default)
    {
        var clamped = Math.Clamp(brightness, 0, 100);
        return Task.Run(() =>
        {
            NativeMethods.EnumDisplayMonitors(nint.Zero, nint.Zero, (hMonitor, _, _, _) =>
            {
                if (!NativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out var count) || count == 0)
                    return true;

                var monitors = new NativeMethods.PhysicalMonitor[count];
                if (!NativeMethods.GetPhysicalMonitorsFromHMONITOR(hMonitor, count, monitors))
                    return true;

                for (int i = 0; i < count; i++)
                {
                    var h = monitors[i].hPhysicalMonitor;
                    try
                    {
                        NativeMethods.SetVCPFeature(h, NativeMethods.MC_VCP_CODE_BRIGHTNESS, (uint)clamped);
                    }
                    finally
                    {
                        NativeMethods.DestroyPhysicalMonitor(h);
                    }
                }

                return true;
            }, nint.Zero);
        }, ct);
    }
}
