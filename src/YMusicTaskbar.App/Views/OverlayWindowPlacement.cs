using System;
using System.Windows;
using System.Windows.Interop;

namespace YMusicTaskbar;

internal sealed class OverlayWindowPlacement(Window window)
{
    private Placement? applied;
    public int Changes { get; private set; }
    public void Apply(Placement target)
    {
        if (window.IsVisible && applied == target) return;
        var helper = new WindowInteropHelper(window); var handle = helper.EnsureHandle();
        IntPtr owner = Native.TaskbarAt(target.X, target.Y);
        if (owner != IntPtr.Zero && helper.Owner != owner) helper.Owner = owner;
        double scale = target.Dpi / 96.0;
        window.Width = target.Width / scale; window.Height = target.Height / scale;
        Set(handle, target); if (!window.IsVisible) window.Show(); Set(handle, target);
        applied = target; Changes++;
    }
    private static void Set(IntPtr handle, Placement target) => Native.SetWindowPos(handle, new IntPtr(-1), target.X, target.Y, target.Width, target.Height, 0x0010);
}
