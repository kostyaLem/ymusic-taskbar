using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace YMusicTaskbar;

// Give tray popups a stable keyboard owner without activating the taskbar overlay.
internal sealed class MenuFocusHost : Window
{
    public MenuFocusHost()
    {
        Width = Height = 1;
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false; AllowsTransparency = true; Background = Brushes.Transparent;
    }

    public void Open(PlayerMenu menu)
    {
        Show();
        var point = System.Windows.Forms.Cursor.Position;
        Native.SetWindowPos(new WindowInteropHelper(this).Handle, IntPtr.Zero, point.X, point.Y, 1, 1, 0x0014);
        Activate();
        menu.PlacementTarget = this;
        menu.Closed += (_, _) => Hide();
        menu.IsOpen = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
        {
            if (menu.IsOpen) menu.Items.OfType<MenuItem>().FirstOrDefault(item => item.IsEnabled)?.Focus();
        }));
    }
}
