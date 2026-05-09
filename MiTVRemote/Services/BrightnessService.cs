namespace MiTVRemote.Services;

/// <summary>
/// 显示器亮度控制服务（通过 DDC/CI 或系统 API）。
/// 一期仅搭建接口骨架，不做完整实现。
/// 参照原 Swift 项目 BrightnessController.swift。
/// </summary>
public class BrightnessService
{
    /// <summary>
    /// 获取显示器当前亮度（0-100）。
    /// 需要实现：通过 dxva2.dll GetMonitorBrightness 或 DDC/CI GetVCPFeature。
    /// </summary>
    public Task<int> GetBrightnessAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException("一期不做完整实现");
    }

    /// <summary>
    /// 设置显示器亮度（0-100）。
    /// 需要实现：通过 dxva2.dll SetMonitorBrightness 或 DDC/CI SetVCPFeature。
    /// </summary>
    public Task SetBrightnessAsync(int brightness, CancellationToken ct = default)
    {
        throw new NotImplementedException("一期不做完整实现");
    }
}
