namespace YMusicTaskbar;

public static class TaskbarRegions
{
    // Reserve actual shell controls, never the task-list or rebar container spanning the free gap.
    public static bool IsReservedShellControl(string className) => className is
        "TrayNotifyWnd" or "TrayClockWClass" or "ClockButton" or "TrayShowDesktopButton" or
        "Start" or "SearchPane" or "SearchEditBoxWrapperClass" or "CortanaUI";

    public static Span? Intersect(PixelRect taskbar, PixelRect control)
    {
        if (control.Width <= 0 || control.Height <= 0 || control.Top >= taskbar.Bottom || control.Bottom <= taskbar.Top) return null;
        int left = System.Math.Max(taskbar.Left, control.Left), right = System.Math.Min(taskbar.Right, control.Right);
        return right > left ? new Span(left, right) : null;
    }
}
