using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YMusicTaskbar;
namespace YMusicTaskbar.AppTests;

internal static class MenuScenarios
{
    public static void Run(Action<bool, string> check)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var messages = new RecordingNotifications();
                int opened = 0, preferences = 0, exited = 0;
                var menu = new PlayerMenu(new Settings { Theme = ThemeMode.Dark }, messages,
                    () => preferences++, () => opened++, () => exited++);
                var items = menu.Items.OfType<MenuItem>().ToArray();
                check(items.Skip(1).Select(i => i.Header).SequenceEqual(new[] { "Открыть музыкальное приложение", "Настройки…", "Выход из панели" }), "Tray menu contains only open, settings and exit actions");
                check(items.All(i => !i.HasItems && !i.IsCheckable), "Menu no longer duplicates configuration options");
                check(items[0].Header is StackPanel heading && heading.Children.Count == 2, "Menu heading contains product name and description only");
                foreach (var item in items.Skip(1)) item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                check(opened == 1 && preferences == 1 && exited == 1, "Menu forwards its three actions");
                var failing = new PlayerMenu(new Settings(), messages, () => throw new InvalidOperationException("settings test"), () => { }, () => { });
                failing.Items.OfType<MenuItem>().Single(i => Equals(i.Header, "Настройки…")).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                check(messages.Messages.Single() == "settings test", "Menu reports action failures");
                menu.Measure(new Size(340, 1000)); menu.Arrange(new Rect(menu.DesiredSize)); menu.UpdateLayout();
                check(menu.ActualWidth >= 294 && menu.ActualWidth <= 340, "Custom menu respects readable width");
                var row = items[2]; row.ApplyTemplate();
                check(row.Template.FindName("Row", row) is Border { CornerRadius.TopLeft: 7 }, "Menu rows retain custom rounded template");
                check(((SolidColorBrush)menu.Background).Color == Color.FromRgb(32, 32, 38), "Menu retains player dark palette");
            }
            catch (Exception exception) { failure = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join();
        if (failure is not null) throw new Exception("Menu scenarios failed", failure);
    }
}