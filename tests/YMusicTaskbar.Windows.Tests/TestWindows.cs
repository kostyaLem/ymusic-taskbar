using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using YMusicTaskbar;
using Forms = System.Windows.Forms;

internal static class TestWindows
{
    public static Window Cover(Forms.Screen screen, bool fullscreen)
    {
        var window = new Window
        {
            Title = "Проверка панели музыкального приложения",
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            ShowActivated = false,
            Background = new SolidColorBrush(Color.FromRgb(25, 25, 30)),
            Content = new TextBlock { Text = "Проверка полноэкранного режима…", Foreground = Brushes.White, FontSize = 24, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        };
        var handle = new WindowInteropHelper(window).EnsureHandle();
        var bounds = screen.Bounds;
        Native.SetWindowPos(handle, IntPtr.Zero, bounds.Left, bounds.Top, fullscreen ? bounds.Width : bounds.Width / 2, fullscreen ? bounds.Height : bounds.Height / 2, 0x0014);
        window.Show();
        Native.SetWindowPos(handle, IntPtr.Zero, bounds.Left, bounds.Top, fullscreen ? bounds.Width : bounds.Width / 2, fullscreen ? bounds.Height : bounds.Height / 2, 0x0014);
        return window;
    }
}
