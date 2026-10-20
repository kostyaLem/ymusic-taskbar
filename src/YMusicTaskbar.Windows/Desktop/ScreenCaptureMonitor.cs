using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;

namespace YMusicTaskbar;

public sealed class ScreenCaptureMonitor : IDisposable
{
    private readonly Native.WinEventProc callback;
    private GCHandle callbackRoot;
    private IntPtr hook;
    public event EventHandler? ForegroundChanged;

    public ScreenCaptureMonitor()
    {
        callback = (_, _, _, _, _, _, _) =>
        {
            try { ForegroundChanged?.Invoke(this, EventArgs.Empty); }
            catch (Exception exception) { Trace.TraceError(exception.ToString()); }
        };
        callbackRoot = GCHandle.Alloc(callback);
        // Foreground events are delivered on the registering thread, which owns the WPF window.
        hook = Native.SetWinEventHook(3, 3, IntPtr.Zero, callback, 0, 0, 2);
    }

    public bool IsSelecting
    {
        get
        {
            var screens = Forms.Screen.AllScreens.Select(s => new PixelRect(s.Bounds.Left, s.Bounds.Top, s.Bounds.Right, s.Bounds.Bottom)).ToArray();
            var processes = new Dictionary<uint, string>();
            bool selecting = false;
            Native.EnumWindows((handle, _) =>
            {
                if (!Native.IsWindowVisible(handle) || Native.IsIconic(handle)) return true;
                if (Native.DwmGetWindowAttribute(handle, 14, out int cloaked, sizeof(int)) == 0 && cloaked != 0) return true;
                if (!Native.GetWindowRect(handle, out var rect)) return true;
                var bounds = new PixelRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
                if (!screens.Any(screen => FullscreenWindowPolicy.CoversMonitor(bounds, screen))) return true;
                Native.GetWindowThreadProcessId(handle, out uint pid);
                if (!processes.TryGetValue(pid, out string? processName))
                {
                    try
                    {
                        using var process = Process.GetProcessById((int)pid);
                        processName = process.ProcessName;
                    }
                    catch (ArgumentException) { processName = ""; }
                    catch (InvalidOperationException) { processName = ""; }
                    catch (System.ComponentModel.Win32Exception) { processName = ""; }
                    processes[pid] = processName;
                }
                if (!screens.Any(screen => ScreenCapturePolicy.IsSelectionOverlay(processName, bounds, screen))) return true;
                selecting = true;
                return false;
            }, IntPtr.Zero);
            return selecting;
        }
    }
    public void Dispose()
    {
        if (hook != IntPtr.Zero && !Native.UnhookWinEvent(hook)) return;
        hook = IntPtr.Zero;
        if (callbackRoot.IsAllocated) callbackRoot.Free();
    }
}