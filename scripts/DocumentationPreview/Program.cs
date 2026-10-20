using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Markup;
using System.Xml.Linq;
using YMusicTaskbar;
using ThemeMode = YMusicTaskbar.ThemeMode;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length != 2) return 2;
        string root = Path.GetFullPath(args[0]), output = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(output);
        File.Delete(Path.Combine(output, "error.txt"));
        // Load the real application's resources without starting its observer or media services.
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var source = XDocument.Load(Path.Combine(root, "src/YMusicTaskbar.App/App.xaml"));
        var dictionary = new XElement(presentation + "ResourceDictionary",
            new XAttribute(XNamespace.Xmlns + "x", "http://schemas.microsoft.com/winfx/2006/xaml"),
            source.Root!.Element(presentation + "Application.Resources")!.Element(presentation + "ResourceDictionary")!.Elements());
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown,
            Resources = (ResourceDictionary)XamlReader.Parse(dictionary.ToString()) };
        int exitCode = 0;
        application.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                using var media = new PreviewMedia(File.ReadAllBytes(Path.Combine(root, "assets/app.png")));
                using var audio = new PreviewAudio(); var settings = new PreviewSettings();
                var model = new PlayerViewModel(media, audio, settings); model.Refresh();
                var window = new PlayerWindow(model);
                try
                {
                    var content = (FrameworkElement)window.Content; window.Content = null;
                    content.DataContext = model; content.Width = 530; content.Height = 42;
                    Layout(content, content.Width);
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    Save(content, Path.Combine(output, "player-full.png"), 16);
                    SocialPreview.Save(content, Path.Combine(root, "assets/app.png"), Path.Combine(output, "social-preview.png"));
                    window.SetCompact(true); content.Width = window.CompactWidth - 10; content.Height = 28;
                    Layout(content, content.Width);
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    Save(content, Path.Combine(output, "player-compact.png"), 16);
                    window.SetCompact(false); window.FitContent(540, 36);
                    var shortTaskbar = new Border { Width = 540, Height = 36, Child = content };
                    Layout(shortTaskbar, 540);
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    Save(shortTaskbar, Path.Combine(output, "player-short-taskbar.png"), 16);
                }
                finally { window.Close(); }
                var preferences = new SettingsWindow(new Settings { Theme = ThemeMode.Light }, new[] { @"\\.\DISPLAY1", @"\\.\DISPLAY2" }, @"\\.\DISPLAY1", false, (_, _) => { });
                var preferencesContent = (FrameworkElement)preferences.Content;
                preferencesContent.Margin = new Thickness(26); preferences.Content = null;
                var preferencesFrame = new Border { Width = preferences.Width, Height = preferences.Height, Background = (Brush)preferences.Resources["PlayerSurface"], Child = preferencesContent };
                preferencesFrame.Resources = preferences.Resources; preferencesFrame.SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty, "PlayerText"); preferencesFrame.SetValue(System.Windows.Documents.TextElement.FontSizeProperty, 13d);
                preferencesFrame.Measure(new Size(preferences.Width, preferences.Height)); preferencesFrame.Arrange(new Rect(0, 0, preferences.Width, preferences.Height)); preferencesFrame.UpdateLayout(); Save(preferencesFrame, Path.Combine(output, "settings-window.png")); preferences.Close();
                var menu = new PlayerMenu(new Settings { Theme = ThemeMode.Dark }, settings, () => { }, () => { }, () => { });
                Layout(menu, 340);
                Save(menu, Path.Combine(output, "settings-menu.png"));
            }
            catch (Exception exception) { File.WriteAllText(Path.Combine(output, "error.txt"), exception.ToString()); exitCode = 1; }
            finally { application.Dispatcher.InvokeShutdown(); }
        });
        Dispatcher.Run(); return exitCode;
    }

    private static void Layout(FrameworkElement element, double width)
    {
        element.Measure(new Size(width, 1000)); element.Arrange(new Rect(element.DesiredSize)); element.UpdateLayout();
    }

    private static void Save(FrameworkElement element, string path, double padding = 0)
    {
        element.UpdateLayout();
        double width = element.ActualWidth + padding * 2, height = element.ActualHeight + padding * 2;
        if (width <= 0 || height <= 0) throw new InvalidOperationException("Empty documentation preview");
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            if (padding > 0) context.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(32, 32, 38)), null, new Rect(0, 0, width, height), 12, 12);
            var brush = new VisualBrush { Stretch = Stretch.Fill, AutoLayoutContent = false, Visual = element };
            context.DrawRectangle(brush, null, new Rect(padding, padding, element.ActualWidth, element.ActualHeight));
        }
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(width * 2), (int)Math.Ceiling(height * 2), 192, 192, PixelFormats.Pbgra32);
        bitmap.Render(visual); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path); encoder.Save(stream);
    }
}
