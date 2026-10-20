using System;
using System.Drawing;
using System.Reflection;
using Forms = System.Windows.Forms;

namespace YMusicTaskbar;

internal sealed class PlayerTray : IDisposable
{
    private readonly Forms.NotifyIcon tray;
    private readonly Icon icon;
    private readonly Func<PlayerMenu> createMenu;
    private PlayerMenu? menu;
    private readonly MenuFocusHost menuHost = new();

    public PlayerTray(Settings settings, IUserNotifications notifications, Action openSettings, Action openMusic, Action exit)
    {
        createMenu = () => new(settings, notifications, openSettings, openMusic, exit);
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("YMusicTaskbar.AppIcon")!;
        using var original = new Icon(stream); icon = (Icon)original.Clone();
        tray = new Forms.NotifyIcon { Icon = icon, Visible = true, Text = "YMusic Taskbar" };
        tray.MouseUp += (_, e) => { if (e.Button is Forms.MouseButtons.Left or Forms.MouseButtons.Right) ShowMenu(); };
    }

    public void ShowMenu()
    {
        if (menu?.IsOpen == true) { menu.IsOpen = false; return; }
        menu = createMenu(); menuHost.Open(menu);
    }
    public void Notify(string value) => tray.ShowBalloonTip(5000, "YMusic Taskbar", value, Forms.ToolTipIcon.Info);
    public void Dispose()
    { if (menu is not null) menu.IsOpen = false; menuHost.Close(); tray.Visible = false; tray.Dispose(); icon.Dispose(); }
}
