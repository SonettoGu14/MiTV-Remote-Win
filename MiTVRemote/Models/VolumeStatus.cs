namespace MiTVRemote.Models;

/// <summary>
/// 电视音量状态。
/// 参照原 Swift 项目 VolumeStatus.swift。
/// </summary>
public record VolumeStatus(int Volume, int MaxVolume)
{
    public int Percent => MaxVolume > 0 ? (int)((double)Volume / MaxVolume * 100) : 0;
}
