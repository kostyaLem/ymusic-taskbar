using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using Forms = System.Windows.Forms;

namespace YMusicTaskbar;

public sealed class DesktopService : IDesktopService
{
    public IReadOnlyList<DesktopSlot> Inspect()
    {
        var bars = new List<IntPtr>(); var fullscreen = new List<Native.Rect>();
        Native.EnumWindows((h, _) =>
        {
            if (!Native.IsWindowVisible(h)) return true;
            string name = Native.ClassName(h);
            if (name is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd") { bars.Add(h); return true; }
            if (name is "Progman" or "WorkerW" || Native.IsIconic(h)) return true;
            Native.GetWindowThreadProcessId(h, out uint pid);
            if (pid == Environment.ProcessId) return true;
            if (Native.DwmGetWindowAttribute(h, 14, out int cloaked, sizeof(int)) == 0 && cloaked != 0) return true;
            uint extendedStyle = unchecked((uint)Native.GetWindowLongPtr(h, -20).ToInt64());
            byte? opacity = null; bool colorKeyed = false;
            if ((extendedStyle & 0x00080000) != 0 && Native.GetLayeredWindowAttributes(h, out uint colorKey, out byte alpha, out uint flags))
            {
                if ((flags & 2) != 0) opacity = alpha;
                colorKeyed = (flags & 1) != 0;
            }
            if (!FullscreenWindowPolicy.CanCoverDesktop(extendedStyle, opacity, colorKeyed)) return true;
            // DWM bounds exclude invisible resize borders that can extend beyond a maximized window.
            bool hasBounds = Native.DwmGetWindowRectAttribute(h, 9, out var rect, Marshal.SizeOf<Native.Rect>()) == 0;
            if (!hasBounds) hasBounds = Native.GetWindowRect(h, out rect);
            if (hasBounds && rect.Width > 0 && rect.Height > 0) fullscreen.Add(rect);
            return true;
        }, IntPtr.Zero);
        var result = new List<DesktopSlot>();
        foreach (var screen in Forms.Screen.AllScreens)
        {
            var sb = screen.Bounds;
            var bounds = new Native.Rect { Left = sb.Left, Top = sb.Top, Right = sb.Right, Bottom = sb.Bottom };
            var bar = bars.FirstOrDefault(h => { Native.GetWindowRect(h, out var r); return r.Left < sb.Right && r.Right > sb.Left && r.Top < sb.Bottom && r.Bottom > sb.Top; });
            Native.Rect taskbar = default;
            if (bar != IntPtr.Zero) Native.GetWindowRect(bar, out taskbar);
            // A hidden auto-hide taskbar normally leaves a 1–2 pixel strip. Never float above it.
            bool visible = bar != IntPtr.Zero && taskbar.Width > taskbar.Height && Math.Min(taskbar.Bottom, bounds.Bottom) - Math.Max(taskbar.Top, bounds.Top) >= taskbar.Height - 4;
            bool covered = fullscreen.Any(r => FullscreenWindowPolicy.CoversMonitor(new(r.Left, r.Top, r.Right, r.Bottom), new(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom)));
            var occupied = new List<Span>(); string? error = null;
            if (visible)
            {
                try
                {
                    var element = AutomationElement.FromHandle(bar);
                    var buttons = element.FindAll(TreeScope.Descendants, new OrCondition(
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit)));
                    foreach (AutomationElement button in buttons)
                    {
                        var current = button.Current;
                        if (current.ProcessId == Environment.ProcessId) continue;
                        var rect = current.BoundingRectangle;
                        if (!current.IsOffscreen && !rect.IsEmpty && rect.Width > 0 && rect.Top < taskbar.Bottom && rect.Bottom > taskbar.Top)
                            occupied.Add(new Span(rect.Left, rect.Right));
                    }
                    bool accessibleControlsFound = occupied.Count > 0;
                    // Windows 10 exposes the clock/tray and search through native child windows too.
                    Native.EnumChildWindows(bar, (child, _) =>
                    {
                        if (!Native.IsWindowVisible(child) || !TaskbarRegions.IsReservedShellControl(Native.ClassName(child))) return true;
                        if (Native.GetWindowRect(child, out var rect) && TaskbarRegions.Intersect(
                            new(taskbar.Left, taskbar.Top, taskbar.Right, taskbar.Bottom),
                            new(rect.Left, rect.Top, rect.Right, rect.Bottom)) is { } region) occupied.Add(region);
                        return true;
                    }, IntPtr.Zero);
                    // Fail closed if Explorer's accessibility tree is not available.
                    if (!accessibleControlsFound) { visible = false; error = "Не удалось определить занятые области панели задач"; }
                }
                catch (Exception ex) { visible = false; error = ex.Message; }
            }
            var monitor = Native.MonitorFromPoint(new Native.Point { X = bounds.Left + 10, Y = bounds.Top + 10 }, 2);
            uint dpi = Native.GetDpiForMonitor(monitor, 0, out uint dx, out _) == 0 ? dx : 96;
            result.Add(new(screen.DeviceName, screen.Primary, new(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom), new(taskbar.Left, taskbar.Top, taskbar.Right, taskbar.Bottom), covered, visible, occupied, error, dpi));
        }
        return result;
    }

    public bool IsMusicRunning()
    {
        var processes = Process.GetProcessesByName(CompatibilityIds.MusicProcess);
        bool running = processes.Length > 0;
        foreach (var process in processes) process.Dispose();
        return running;
    }
    public void OpenMusicWindow()
    {
        foreach (var process in Process.GetProcessesByName(CompatibilityIds.MusicProcess))
            using (process) if (process.MainWindowHandle != IntPtr.Zero)
            { Native.ShowWindow(process.MainWindowHandle, 9); Native.SetForegroundWindow(process.MainWindowHandle); return; }
    }
}
