namespace MiTVRemote.Models;

/// <summary>
/// 局域网中发现的 MiTV 设备。
/// 参照原 Swift 项目 MiTVDevice.swift。
/// </summary>
public record MiTvDevice(string Name, string Host);
