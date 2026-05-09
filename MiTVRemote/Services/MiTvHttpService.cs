using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MiTVRemote.Models;

namespace MiTVRemote.Services;

/// <summary>
/// MiTV HTTP API 客户端实现。
/// 所有操作基于 GET 请求到 http://{host}:6095/...。
/// 参照原 Swift 项目 MiTVController.swift。
/// </summary>
public class MiTvHttpService : IMiTvService, IDisposable
{
    private readonly HttpClient _http;

    public MiTvHttpService()
    {
        var handler = new SocketsHttpHandler
        {
            ConnectTimeout = TimeSpan.FromMilliseconds(450),
            MaxConnectionsPerServer = int.MaxValue
        };
        _http = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(2)
        };
    }

    public async Task<bool> IsAliveAsync(string host, CancellationToken ct = default)
    {
        var url = $"http://{host}:6095/request?action=isalive";
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("data", out var data) &&
               data.TryGetProperty("device", out var dev) &&
               !string.IsNullOrEmpty(dev.GetString());
    }

    public async Task<VolumeStatus?> GetVolumeAsync(string host, CancellationToken ct = default)
    {
        var url = $"http://{host}:6095/controller?action=getvolume";
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("data", out var data))
        {
            int volume = data.TryGetProperty("volume", out var v) ? v.GetInt32() : 0;
            int max = data.TryGetProperty("maxVolume", out var m) ? m.GetInt32() : 100;
            return new VolumeStatus(volume, max);
        }
        return null;
    }

    public async Task<bool> SetVolumeAsync(string host, int volume, string signingMac, CancellationToken ct = default)
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signInput = $"mitvsignsalt{signingMac}setVolum{volume}{ts}";
        var sign = ComputeMd5Hex(signInput);

        var url = $"http://{host}:6095/general?" +
                  $"action=setVolum&volum={volume}&ts={ts}&sign={sign}";

        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("data", out var data) &&
               data.TryGetProperty("success", out var s) &&
               s.GetBoolean();
    }

    public async Task SendKeyAsync(string host, string keyCode, CancellationToken ct = default)
    {
        var url = $"http://{host}:6095/controller?action=keyevent&keycode={keyCode}";
        await _http.GetAsync(url, ct);
    }

    public async Task<bool> ChangeSourceAsync(string host, string sourceName, CancellationToken ct = default)
    {
        var url = $"http://{host}:6095/controller?action=changesource&source={Uri.EscapeDataString(sourceName)}";
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("data", out var data) &&
               data.TryGetProperty("success", out var s) &&
               s.GetBoolean();
    }

    public async Task<DeviceSystemInfo?> GetSystemInfoAsync(string host, CancellationToken ct = default)
    {
        var url = $"http://{host}:6095/controller?action=getsysteminfo";
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("data", out var data))
        {
            var wifi = data.TryGetProperty("wifiMAC", out var w) ? w.GetString() : null;
            var eth = data.TryGetProperty("ethernetMAC", out var e) ? e.GetString() : null;
            return new DeviceSystemInfo(wifi, eth);
        }
        return null;
    }

    public async Task<bool> IsReachableAsync(string host, CancellationToken ct = default)
    {
        var url = $"http://{host}:6095/controller?action=getinstalledapp&count=1";
        var response = await _http.GetAsync(url, ct);
        return response.IsSuccessStatusCode;
    }

    public void Dispose()
    {
        _http.Dispose();
    }

    private static string ComputeMd5Hex(string input)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hash);
    }
}
