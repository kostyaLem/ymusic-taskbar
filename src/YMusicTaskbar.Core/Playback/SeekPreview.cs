using System;

namespace YMusicTaskbar;

public sealed class SeekPreview
{
    private double? target;
    private DateTimeOffset started;
    public void Begin(double value, DateTimeOffset now) { target = value; started = now; }
    public void Cancel() => target = null;
    public double Display(double actual, bool playing, double duration, DateTimeOffset now)
    {
        if (target is not { } requested) return actual;
        double age = Math.Max(0, (now - started).TotalSeconds);
        double expected = Math.Clamp(requested + (playing ? age : 0), 0, Math.Max(0, duration));
        if (Math.Abs(actual - expected) <= 0.75 || age >= 3) { Cancel(); return actual; }
        return expected;
    }
}
