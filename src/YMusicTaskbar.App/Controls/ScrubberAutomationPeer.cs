using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Windows.Media;

namespace YMusicTaskbar;

internal sealed class ScrubberAutomationPeer(Scrubber owner) : FrameworkElementAutomationPeer(owner), IRangeValueProvider
{
    protected override string GetClassNameCore() => "Scrubber";
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;
    public override object? GetPattern(PatternInterface pattern) => pattern == PatternInterface.RangeValue ? this : base.GetPattern(pattern);
    public bool IsReadOnly => !owner.IsEnabled;
    public double LargeChange => owner.Step * 5;
    public double SmallChange => owner.Step;
    public double Maximum => owner.Maximum;
    public double Minimum => 0;
    public double Value => owner.Value;
    public void SetValue(double value)
    {
        if (!owner.IsEnabled) throw new ElementNotEnabledException();
        owner.CommitUserValue(value);
    }
}
