using System.Windows;
using System.Windows.Media;
using YMusicTaskbar;

namespace YMusicTaskbar.AppTests;

internal static class LayoutScenarios
{
    public static void Run(Action<bool, string> check)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            App? application = null; PlayerWindow? window = null;
            try
            {
                application = new App(); application.InitializeComponent();
                var media = new FakeMediaService();
                var model = new PlayerViewModel(media, new FakeAudioService(), new RecordingNotifications()); model.Refresh();
                window = new PlayerWindow(model);
                var root = (FrameworkElement)window.Content;
                Rect Bounds(string name)
                {
                    if (name == "Track" && ((FrameworkElement)window.FindName("FullMetadata")).Visibility == Visibility.Visible) name = "FullMetadata";
                    var element = (FrameworkElement)window.FindName(name);
                    return root.LayoutTransform.TransformBounds(element.TransformToAncestor(root).TransformBounds(new Rect(element.RenderSize)));
                }
                void Arrange(double width, double height, bool compact)
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.DataBind);
                    window.SetCompact(compact); window.FitContent(width, height);
                    root.Measure(new Size(width, height)); root.Arrange(new Rect(0, 0, width, height)); root.UpdateLayout();
                }
                var settings = new Settings { Theme = ThemeMode.Light, Compact = true };
                settings.Monitors["Screen A"] = new MonitorProfile { Side = PlayerSide.Right, Compact = true };
                Settings? saved = null; bool launch = false; int diagnostics = 0; bool failDiagnostics = false;
                var dialog = new SettingsWindow(settings, new[] { "Screen A", "Screen B", "Screen C" }, "Screen A", false, (draft, startup) => { saved = draft; launch = startup; }, () => { diagnostics++; return failDiagnostics ? Task.FromException(new System.IO.IOException("diagnostic test")) : Task.CompletedTask; });
                check(!dialog.ShowInTaskbar, "Settings window never adds a taskbar button that could resize the overlay");
                var diagnosticButton = (System.Windows.Controls.Button)dialog.FindName("DiagnosticsButton");
                diagnosticButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                check(diagnostics == 1 && saved is null && diagnosticButton.IsEnabled, "Diagnostics runs from settings without applying pending edits");
                failDiagnostics = true;
                diagnosticButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                check(((System.Windows.Controls.TextBlock)dialog.FindName("ErrorText")).Text == "diagnostic test" && diagnosticButton.IsEnabled, "Diagnostic failure is displayed and button becomes available again");
                var choice = (System.Windows.Controls.ComboBox)dialog.FindName("ThemeChoice");
                choice.SelectedIndex = (int)ThemeMode.Dark;
                check(settings.Theme == ThemeMode.Light && saved is { Theme: ThemeMode.Dark }, "Settings edits apply immediately without mutating the opening snapshot");
                check(((SolidColorBrush)dialog.Resources["PlayerText"]).Color.R > 200, "Settings theme preview updates immediately");
                var selector = (System.Windows.Controls.ListBox)dialog.FindName("MonitorSelector");
                var side = (System.Windows.Controls.ComboBox)dialog.FindName("SideChoice");
                var layout = (System.Windows.Controls.ComboBox)dialog.FindName("LayoutChoice");
                check(selector.Items.Count == 3 && selector.SelectedIndex == 0, "Monitor tiles show all displays and select the preferred display");
                check(side.SelectedIndex == 1 && layout.SelectedIndex == 1, "Shared editor restores selected monitor profile");
                selector.SelectedIndex = 1;
                side.SelectedIndex = 0; layout.SelectedIndex = 0;
                selector.SelectedIndex = 2;
                side.SelectedIndex = 1;
                ((System.Windows.Controls.CheckBox)dialog.FindName("EnabledChoice")).IsChecked = false;
                selector.SelectedIndex = 0;
                check(side.SelectedIndex == 1 && layout.SelectedIndex == 1, "Switching monitor tiles preserves independent in-progress values");
                selector.SelectedIndex = 1;
                check(side.SelectedIndex == 0 && layout.SelectedIndex == 0, "Switching back restores the second monitor edits");
                var scrollStyle = dialog.TryFindResource(typeof(System.Windows.Controls.Primitives.ScrollBar)) as Style;
                check(scrollStyle is not null, "Settings scrolling uses an explicit themed scrollbar style");
                foreach (var orientation in new[] { System.Windows.Controls.Orientation.Vertical, System.Windows.Controls.Orientation.Horizontal })
                {
                    var bar = new System.Windows.Controls.Primitives.ScrollBar { Style = scrollStyle, Orientation = orientation, Minimum = 0, Maximum = 200, ViewportSize = 40, Value = 50 };
                    bar.Measure(orientation == System.Windows.Controls.Orientation.Vertical ? new Size(9, 120) : new Size(120, 9));
                    bar.Arrange(orientation == System.Windows.Controls.Orientation.Vertical ? new Rect(0, 0, 9, 120) : new Rect(0, 0, 120, 9)); bar.UpdateLayout();
                    var rail = (System.Windows.Controls.Primitives.Track)bar.Template.FindName("PART_Track", bar);
                    rail.Thumb.RaiseEvent(new System.Windows.Controls.Primitives.DragStartedEventArgs(0, 0) { RoutedEvent = System.Windows.Controls.Primitives.Thumb.DragStartedEvent });
                    rail.Thumb.RaiseEvent(new System.Windows.Controls.Primitives.DragDeltaEventArgs(orientation == System.Windows.Controls.Orientation.Horizontal ? 10 : 0, orientation == System.Windows.Controls.Orientation.Vertical ? 10 : 0) { RoutedEvent = System.Windows.Controls.Primitives.Thumb.DragDeltaEvent });
                    rail.Thumb.RaiseEvent(new System.Windows.Controls.Primitives.DragCompletedEventArgs(0, 0, false) { RoutedEvent = System.Windows.Controls.Primitives.Thumb.DragCompletedEvent });
                    check(bar.Value > 50, $"Custom {orientation} scrollbar responds to dragging in the expected direction");
                }
                ((System.Windows.Controls.CheckBox)dialog.FindName("StartupChoice")).IsChecked = true;
                var footer = (System.Windows.Controls.Grid)((System.Windows.Controls.Grid)dialog.Content).Children[2];
                var buttons = (System.Windows.Controls.StackPanel)footer.Children[1];
                ((System.Windows.Controls.Button)buttons.Children[1]).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                check(saved is { Theme: ThemeMode.Dark } && launch && saved.Monitors["Screen A"].Compact && !saved.Monitors["Screen B"].Compact && !saved.Monitors["Screen C"].Enabled && saved.Monitors["Screen C"].Side == PlayerSide.Right, "OK retains immediately applied theme, startup and independent monitor profiles");
                check(settings.Monitors.Count == 1 && settings.Monitors["Screen A"].Compact, "Editing clone does not mutate original profiles");
                SettingsScenarios.Run(check);
                ThemeService.Apply(window.Resources, ThemeMode.Light);
                check(((SolidColorBrush)((System.Windows.Controls.TextBlock)window.FindName("Track")).Foreground).Color.R < 80, "Light player theme uses dark readable text");
                check(((SolidColorBrush)((System.Windows.Shapes.Path)window.FindName("PlayIcon")).Fill).Color.R < 180, "Light accent is dark enough for a light taskbar");
                ThemeService.Apply(window.Resources, ThemeMode.Dark);
                check(((SolidColorBrush)((System.Windows.Controls.TextBlock)window.FindName("Track")).Foreground).Color.R > 200, "Dark player theme restores light text");
                Arrange(540, 46, false);
                var fullText = (System.Windows.Controls.TextBlock)window.FindName("FullMetadata");
                check(fullText.Visibility == Visibility.Visible && ((FrameworkElement)window.FindName("CompactMetadata")).Visibility == Visibility.Collapsed && fullText.FontSize >= 12,
                    "Full mode uses larger shared track and artist text");
                check(fullText.ActualHeight <= 15, "Short track and artist stay on a single line in full mode");
                media.Title = "Очень длинное название песни, которое не помещается в одну строку";
                media.Artist = "Исполнитель с длинным именем"; model.Refresh();
                Arrange(360, 36, false);
                check(fullText.ActualHeight > 14 && fullText.ActualHeight <= 30 && Bounds("Track").Bottom <= Bounds("Position").Top,
                    $"Long combined metadata wraps within two lines without overlapping seek: height={fullText.ActualHeight}, content={fullText.Text}, model={model.Title}, text={Bounds("Track")}, seek={Bounds("Position")}");
                check(Bounds("Position").Right >= Bounds("More").Right - 4, "Seek track extends under the three-dot menu button");
                Arrange(360, 32, true);
                check(((FrameworkElement)window.FindName("CompactMetadata")).Visibility == Visibility.Visible && fullText.Visibility == Visibility.Collapsed,
                    "Compact mode retains separate title and artist rows");
                media.Title = "Проверочный трек"; media.Artist = "Исполнитель"; model.Refresh();
                foreach (bool compact in new[] { false, true })
                {
                    double width = compact ? 360 : 540, height = compact ? 32 : 36;
                    Arrange(width, height, compact);
                    var expectedVolume = Bounds("Volume"); var expectedTrack = Bounds("Track");
                    var expectedScale = root.LayoutTransform.Value;
                    var brush = window.Resources["PlayerText"];
                    var buttonStyle = ((System.Windows.Controls.Button)window.FindName("More")).Style;
                    foreach (var theme in new[] { ThemeMode.Light, ThemeMode.Dark, ThemeMode.Light, ThemeMode.Dark })
                    {
                        ThemeService.Apply(window.Resources, theme);
                        check(ReferenceEquals(brush, window.Resources["PlayerText"]), $"{compact}: theme keeps existing brush instances");
                        root.UpdateLayout();
                        check(root.LayoutTransform.Value == expectedScale && Bounds("Volume") == expectedVolume && Bounds("Track") == expectedTrack,
                            $"{compact}: theme never changes control bounds or content scale");
                        check(ReferenceEquals(buttonStyle, ((System.Windows.Controls.Button)window.FindName("More")).Style), $"{compact}: theme retains button style");
                    }
                }
                Arrange(540, 36, false);
                var track = Bounds("Track"); var seek = Bounds("Position"); var volume = Bounds("Volume");
                check(track.Bottom <= seek.Top && seek.Bottom <= 36, "Windows 10 full layout keeps seek below track and inside short taskbar");
                check(volume.Top >= 0 && volume.Bottom <= 36, "Windows 10 full layout keeps volume inside taskbar");
                check(root.LayoutTransform.Value.M11 < 1, "Short taskbar scales content uniformly");
                Arrange(360, 26, true);
                check(((FrameworkElement)window.FindName("Position")).Visibility == Visibility.Collapsed, "Small Windows 10 taskbar retains compact layout");
                check(Bounds("Volume").Bottom <= 26 && Bounds("Mute").Bottom <= 26, "Small taskbar keeps compact controls inside bounds");
                Arrange(540, 46, false);
                check(root.LayoutTransform.Value == Matrix.Identity, "Normal Windows 11 taskbar restores unscaled layout");
            }
            catch (Exception exception) { failure = exception; }
            finally { window?.Close(); application?.Shutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join();
        if (failure is not null) throw new Exception("Taskbar layout scenarios failed", failure);
    }
}
