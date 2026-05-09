using System.Windows;
using System.Windows.Controls;

namespace MiTVRemote.UI;

/// <summary>
/// 自定义音量滑块控件。
/// 参照原 Swift 项目 VolumeSlider.swift —— 拖拽过程中持续触发 onDrag 回调，
/// 而非仅在松开鼠标时触发。WPF Slider 默认已支持，此控件为扩展点。
/// </summary>
public partial class VolumeSliderControl : UserControl
{
    public VolumeSliderControl()
    {
        InitializeComponent();
    }

    // TODO: 实现拖拽持续触发逻辑（如 IsMoveToPointEnabled, 自定义 Thumb.DragDelta 等）
}
