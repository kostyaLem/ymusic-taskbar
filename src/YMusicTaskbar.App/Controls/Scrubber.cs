using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Windows.Media;

namespace YMusicTaskbar;

public sealed class Scrubber : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(Scrubber), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(Scrubber), new FrameworkPropertyMetadata(100d, FrameworkPropertyMetadataOptions.AffectsRender));
    public double Value { get => (double)GetValue(ValueProperty); set => SetCurrentValue(ValueProperty, value); }
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double Step { get; set; } = 1;
    public bool IsInteracting { get; private set; }
    public event EventHandler? UserValueChanged;
    public event EventHandler? Committed;
    public event EventHandler? InteractionStarted;
    public event EventHandler? InteractionEnded;
    public static readonly DependencyProperty RailBrushProperty = DependencyProperty.Register("RailBrush", typeof(Brush), typeof(Scrubber), new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.Register("AccentBrush", typeof(Brush), typeof(Scrubber), new FrameworkPropertyMetadata(Brushes.Gold, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty ThumbBrushProperty = DependencyProperty.Register("ThumbBrush", typeof(Brush), typeof(Scrubber), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));
    public Scrubber()
    {
        SetResourceReference(RailBrushProperty, "PlayerRail");
        SetResourceReference(AccentBrushProperty, "PlayerAccent");
        SetResourceReference(ThumbBrushProperty, "PlayerText");
        Focusable = true; Cursor = Cursors.Hand;
        IsEnabledChanged += (_, _) => InvalidateVisual();
        MouseEnter += (_, _) => InvalidateVisual(); MouseLeave += (_, _) => InvalidateVisual();
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
        double left = 5, right = Math.Max(left, ActualWidth - 5), y = ActualHeight / 2;
        double x = left + (right - left) * Math.Clamp(Value / Math.Max(1, Maximum), 0, 1);
        dc.PushOpacity(IsEnabled ? 1 : 0.25);
        dc.DrawRoundedRectangle((Brush)GetValue(RailBrushProperty), null, new Rect(left, y - 1.5, right - left, 3), 1.5, 1.5);
        dc.DrawRoundedRectangle((Brush)GetValue(AccentBrushProperty), null, new Rect(left, y - 1.5, Math.Max(0, x - left), 3), 1.5, 1.5);
        dc.DrawEllipse((Brush)GetValue(ThumbBrushProperty), null, new Point(x, y), IsMouseOver || IsInteracting ? 4 : 2.5, IsMouseOver || IsInteracting ? 4 : 2.5);
        dc.Pop();
    }
    private void FromPointer(MouseEventArgs e) => Change(Policy.ValueAtX(e.GetPosition(this).X, ActualWidth, Maximum));
    private void Change(double value)
    {
        Value = Math.Clamp(value, 0, Math.Max(0, Maximum));
        UserValueChanged?.Invoke(this, EventArgs.Empty);
    }
    public void CommitUserValue(double value)
    {
        if (!IsEnabled) return;
        Change(value); Committed?.Invoke(this, EventArgs.Empty);
    }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e); e.Handled = true;
        Focus(); IsInteracting = true; InteractionStarted?.Invoke(this, EventArgs.Empty); CaptureMouse(); FromPointer(e);
    }
    protected override void OnMouseMove(MouseEventArgs e)
    { base.OnMouseMove(e); if (IsInteracting && IsMouseCaptured) { FromPointer(e); e.Handled = true; } }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!IsInteracting) return;
        FromPointer(e); IsInteracting = false; ReleaseMouseCapture();
        e.Handled = true; Committed?.Invoke(this, EventArgs.Empty); InvalidateVisual();
    }
    protected override void OnLostMouseCapture(MouseEventArgs e)
    { base.OnLostMouseCapture(e); IsInteracting = false; InteractionEnded?.Invoke(this, EventArgs.Empty); InvalidateVisual(); }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        double? value = e.Key switch { Key.Left or Key.Down => Value - Step, Key.Right or Key.Up => Value + Step, Key.Home => 0, Key.End => Maximum, _ => null };
        if (value is null) return;
        e.Handled = true; CommitUserValue(value.Value);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new ScrubberAutomationPeer(this);
}
