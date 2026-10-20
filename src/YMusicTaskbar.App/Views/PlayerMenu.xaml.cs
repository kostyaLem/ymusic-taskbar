using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace YMusicTaskbar;

public partial class PlayerMenu : ContextMenu
{
    public PlayerMenu(Settings settings, IUserNotifications notifications, Action openSettings, Action openMusic, Action exit)
    {
        InitializeComponent();
        ThemeService.Apply(Resources, settings.Theme);
        var heading = new StackPanel();
        heading.Children.Add(new TextBlock { Text = "YMusic Taskbar", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = (Brush)Resources["PlayerAccent"] });
        heading.Children.Add(new TextBlock { Text = "Неофициальный мини-плеер для Яндекс Музыки", FontSize = 11, Margin = new Thickness(0, 5, 0, 0), TextWrapping = TextWrapping.Wrap });
        Items.Add(new MenuItem { Header = heading, IsEnabled = false });
        Items.Add(new Separator { Style = (Style)Resources["MenuSeparator"] });
        Add("Открыть музыкальное приложение", openMusic);
        Add("Настройки…", openSettings);
        Items.Add(new Separator { Style = (Style)Resources["MenuSeparator"] });
        Add("Выход из панели", exit);

        void Add(string label, Action action)
        {
            var item = new MenuItem { Header = label };
            item.Click += (_, _) => { try { action(); } catch (Exception exception) { notifications.Report(exception); } };
            Items.Add(item);
        }
    }
}