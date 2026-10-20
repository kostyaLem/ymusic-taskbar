using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace YMusicTaskbar;

internal static class PlayerViewDiagnostics
{
    public static object Capture(PlayerWindow window) => new
    {
        Compact = window.Position.Visibility == Visibility.Collapsed,
        window.PlacementChanges,
        Transparent = window.AllowsTransparency,
        TextColor = (window.Track.Foreground as SolidColorBrush)?.Color.ToString(),
        SeekVisible = window.Position.IsVisible,
        SeekValue = window.Position.Value,
        Width = window.ActualWidth,
        Height = window.ActualHeight,
        VolumeValue = window.Volume.Value,
        IdleHighlightedButtons = new[] { window.Previous, window.PlayPause, window.Next, window.Mute, window.More }
            .Count(b => !b.IsMouseOver && !b.IsPressed && b.Template.FindName("bg", b) is Border border && border.Background is SolidColorBrush brush && brush.Color.A > 0),
        Handle = new WindowInteropHelper(window).Handle.ToInt64()
    };
}
