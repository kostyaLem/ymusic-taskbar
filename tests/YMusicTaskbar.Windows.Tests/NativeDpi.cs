using System;
using System.Runtime.InteropServices;

internal static class NativeDpi
{
    [DllImport("user32.dll")] internal static extern bool SetProcessDpiAwarenessContext(IntPtr context);
}
