using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

internal static class SocialPreview
{
    public static void Save(FrameworkElement player, string logo, string path)
    {
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var background = new LinearGradientBrush(Color.FromRgb(17, 17, 23), Color.FromRgb(38, 38, 49), 30);
            context.DrawRectangle(background, null, new Rect(0, 0, 1280, 640));
            context.DrawImage(BitmapFrame.Create(new Uri(logo)), new Rect(64, 70, 80, 80));
            Text(context, "YMusic Taskbar", 48, Brushes.White, 166, 78, FontWeights.SemiBold);
            Text(context, "Неофициальный мини-плеер для Windows 10 / 11", 32, Brushes.White, 64, 202);
            Text(context, "Треки  ·  Пауза  ·  Перемотка  ·  Громкость", 23, new SolidColorBrush(Color.FromRgb(255, 214, 65)), 64, 258);
            context.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(32, 32, 38)), new Pen(new SolidColorBrush(Color.FromRgb(60, 60, 72)), 1), new Rect(64, 332, 1152, 144), 18, 18);
            var brush = new VisualBrush { AutoLayoutContent = false, Visual = player };
            context.DrawRectangle(brush, null, new Rect(90, 360, 1100, 88));
            Text(context, "Windows 10 / 11  ·  x64  ·  MIT  ·  Открытый исходный код", 22, new SolidColorBrush(Color.FromRgb(176, 176, 190)), 64, 548);
        }
        var bitmap = new RenderTargetBitmap(1280, 640, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path); encoder.Save(stream);
    }

    private static void Text(DrawingContext context, string value, double size, Brush color, double x, double y, FontWeight? weight = null)
    {
        var text = new FormattedText(value, CultureInfo.GetCultureInfo("ru-RU"), FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight ?? FontWeights.Normal, FontStretches.Normal), size, color, 1);
        context.DrawText(text, new Point(x, y));
    }
}
