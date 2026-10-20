using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace YMusicTaskbar;

public partial class PlayerWindow : Window
{
    private readonly PlayerViewModel viewModel;
    private readonly OverlayWindowPlacement placement;
    public bool AudioInteracting => Volume.IsInteracting;
    public int PlacementChanges => placement.Changes;
    public PlayerWindow(PlayerViewModel viewModel)
    {
        this.viewModel = viewModel; InitializeComponent(); DataContext = viewModel;
        placement = new OverlayWindowPlacement(this);
        Position.InteractionStarted += (_, _) => viewModel.BeginSeek();
        Position.InteractionEnded += (_, _) => viewModel.EndSeek();
        Position.UserValueChanged += (_, _) => viewModel.PreviewSeek(Position.Value);
        Position.Committed += async (_, _) => await viewModel.SeekAsync(Position.Value);
        Volume.InteractionStarted += (_, _) => viewModel.IsChangingVolume = true;
        Volume.InteractionEnded += (_, _) => { viewModel.IsChangingVolume = false; viewModel.RefreshAudio(); };
        Volume.UserValueChanged += (_, _) => viewModel.SetVolume(Volume.Value);
    }
    public void SetCompact(bool compact)
    {
        if (Position.Visibility == (compact ? Visibility.Collapsed : Visibility.Visible)) return;
        ArtworkFrame.Width = ArtworkFrame.Height = compact ? 28 : 38;
        ArtworkColumn.Width = new GridLength(compact ? 30 : 40);
        ArtworkFrame.VerticalAlignment = compact ? VerticalAlignment.Center : VerticalAlignment.Top;
        CompactMetadata.Visibility = compact ? Visibility.Visible : Visibility.Collapsed;
        FullMetadata.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        Position.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        SeekRow.Height = new GridLength(compact ? 0 : 10);
        TextColumn.Width = compact ? GridLength.Auto : new GridLength(1, GridUnitType.Star);
        TextColumn.MaxWidth = compact ? 176 : double.PositiveInfinity;
    }
    public int CompactWidth
    {
        get
        {
            double Measure(string text, double size) => new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface(Track.FontFamily, FontStyles.Normal, FontWeights.Medium, FontStretches.Normal), size, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip).WidthIncludingTrailingWhitespace;
            return (int)Math.Ceiling(266 + Math.Clamp(Math.Max(Measure(viewModel.Title, 11.5), Measure(viewModel.Artist, 10)), 32, 160));
        }
    }
    public void Place(Placement target)
    {
        FitContent(target.Width / (target.Dpi / 96.0), target.Height / (target.Dpi / 96.0));
        if (Position.Visibility == Visibility.Collapsed) TextColumn.MaxWidth = Math.Clamp(target.Width / (target.Dpi / 96.0) - 250, 48, 176);
        placement.Apply(target);
    }
    public void FitContent(double width, double height)
    {
        double designHeight = Position.Visibility == Visibility.Collapsed ? 28 : 42;
        double scale = Math.Clamp((height - 4) / designHeight, 0.1, 1);
        if (Math.Abs(Layout.LayoutTransform.Value.M11 - scale) > 0.0001)
            Layout.LayoutTransform = scale < 1 ? new ScaleTransform(scale, scale) : Transform.Identity;
        Layout.Width = Math.Max(1, (width - 10) / scale);
        Layout.Height = designHeight;
    }
    private void OpenMusic(object sender, MouseButtonEventArgs args) => viewModel.OpenMusic.Execute(null);
    public object UiState() => PlayerViewDiagnostics.Capture(this);
    public void SavePreview(string path) => WindowPreview.Save(this, path);
}
