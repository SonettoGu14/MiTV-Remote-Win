namespace MiTVRemote.Models;

/// <summary>
/// MiTV 遥控器按键码。
/// 通过 HTTP API 发送：GET /controller?action=keyevent&amp;keycode={keycode}
/// 参照原 Swift 项目 CECCommand.swift 中的 key code 映射。
/// </summary>
public static class KeyCode
{
    public const string Up = "up";
    public const string Down = "down";
    public const string Left = "left";
    public const string Right = "right";
    public const string Enter = "enter";
    public const string Home = "home";
    public const string Back = "back";
    public const string Menu = "menu";
    public const string Power = "power";
    public const string VolumeUp = "volumeup";
    public const string VolumeDown = "volumedown";
}
