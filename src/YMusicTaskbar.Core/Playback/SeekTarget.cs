using System;

namespace YMusicTaskbar;

public static class SeekTarget
{
    public static double? Normalize(double requested, double duration)
    {
        if (!double.IsFinite(requested) || !double.IsFinite(duration) || duration <= 0) return null;
        // the music service's media-session handler ignores zero; its end-of-track path can overlap players.
        double start = Math.Min(0.1, duration / 4);
        double end = duration - Math.Min(0.5, duration / 4);
        return Math.Clamp(requested, start, end);
    }
}
