using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace YMusicTaskbar;

public sealed class AudioService : IAudioService
{
    private readonly Dictionary<string, AudioSessionSubscription> sessions = new();
    private readonly Guid writeContext = Guid.NewGuid();
    private readonly ConcurrentQueue<VolumeNotification> recent = new();
    private long received, external, own, applied;
    private bool disposed;
    public event EventHandler<VolumeNotification>? VolumeChanged;
    public event EventHandler? SessionsInvalidated;
    public bool Available => sessions.Count > 0;
    public double Level { get; private set; }
    public bool Muted { get; private set; }
    public int SubscriptionCount => sessions.Count;
    public int RegistrationsCreated { get; private set; }
    public long NotificationCount => Interlocked.Read(ref received);
    public long ExternalNotificationCount => Interlocked.Read(ref external);
    public long OwnNotificationCount => Interlocked.Read(ref own);
    public long AppliedNotificationCount => Interlocked.Read(ref applied);
    public VolumeNotification[] RecentNotifications => recent.ToArray();

    // Native calls and reconciliation run on the owning thread. Callbacks only publish immutable data.
    public void Refresh()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var seen = new HashSet<string>();
        var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
        IMMDeviceCollection? devices = null;
        try
        {
            ComInterop.Check(enumerator.EnumAudioEndpoints(0, 1, out devices));
            ComInterop.Check(devices.GetCount(out uint count));
            for (uint d = 0; d < count; d++)
            {
                IMMDevice? device = null; object? managerObject = null; IAudioSessionEnumerator? list = null;
                try
                {
                    ComInterop.Check(devices.Item(d, out device));
                    var iid = typeof(IAudioSessionManager2).GUID;
                    ComInterop.Check(device.Activate(ref iid, 23, IntPtr.Zero, out managerObject));
                    ComInterop.Check(((IAudioSessionManager2)managerObject).GetSessionEnumerator(out list));
                    ComInterop.Check(list.GetCount(out int n));
                    for (int i = 0; i < n; i++)
                    {
                        IAudioSessionControl2? control = null; bool keep = false;
                        try
                        {
                            ComInterop.Check(list.GetSession(i, out control));
                            ComInterop.Check(control.GetProcessId(out uint pid));
                            if (pid == 0) continue;
                            using var process = Process.GetProcessById((int)pid);
                            if (!string.Equals(process.ProcessName, CompatibilityIds.MusicProcess, StringComparison.OrdinalIgnoreCase)) continue;
                            ComInterop.Check(control.GetState(out int state));
                            if (state == 2 || control is not ISimpleAudioVolume volume) continue;
                            ComInterop.Check(control.GetSessionInstanceIdentifier(out string id));
                            if (!sessions.TryGetValue(id, out var binding))
                            {
                                binding = new AudioSessionSubscription(id, control, volume, Receive, Invalidate, writeContext);
                                try { binding.Register(); }
                                catch { binding.Sink.Deactivate(); throw; }
                                sessions.Add(id, binding); keep = true; RegistrationsCreated++;
                            }
                            binding.Active = state == 1;
                            binding.Read(); seen.Add(id);
                        }
                        catch (ArgumentException) { } // The renderer may exit during discovery.
                        finally { if (!keep) ComInterop.Release(control); }
                    }
                }
                finally { ComInterop.Release(list); ComInterop.Release(managerObject); ComInterop.Release(device); }
            }
            foreach (string id in sessions.Keys.Where(id => !seen.Contains(id)).ToArray())
            { var old = sessions[id]; sessions.Remove(id); old.Dispose(); }
            UpdateSelected();
        }
        finally { ComInterop.Release(devices); ComInterop.Release(enumerator); }
    }

    private void Receive(VolumeNotification change)
    {
        Interlocked.Increment(ref received);
        if (change.IsOwnChange) Interlocked.Increment(ref own); else Interlocked.Increment(ref external);
        recent.Enqueue(change); while (recent.Count > 40) recent.TryDequeue(out _);
        VolumeChanged?.Invoke(this, change);
    }
    private void Invalidate() => SessionsInvalidated?.Invoke(this, EventArgs.Empty);

    // Dispatch this to the UI/owner thread; never touch COM objects from inside the callback.
    public bool AcceptNotification(VolumeNotification change)
    {
        if (disposed || change.IsOwnChange || !sessions.TryGetValue(change.SessionId, out var binding)
            || binding.Token != change.Registration || change.Timestamp < binding.LastRead) return false;
        binding.Level = change.Level; binding.Muted = change.Muted; binding.LastRead = change.Timestamp;
        UpdateSelected(); Interlocked.Increment(ref applied); return true;
    }
    private void UpdateSelected()
    {
        var selected = sessions.Values.OrderByDescending(s => s.Active).FirstOrDefault();
        if (selected is null) { Level = 0; Muted = false; return; }
        Level = selected.Level; Muted = selected.Muted;
    }
    public void SetLevel(double value, bool unmute = false)
    {
        if (!Available) throw new InvalidOperationException("Аудиосессия музыкального приложения пока недоступна.");
        double level = Math.Clamp(value, 0, 100);
        foreach (var session in sessions.Values)
        {
            ComInterop.Check(session.Volume.SetMasterVolume((float)(level / 100), writeContext));
            session.Level = level; session.LastRead = Stopwatch.GetTimestamp();
        }
        Level = level;
        if (unmute && Level > 0 && Muted) SetMuted(false);
    }
    public void SetMuted(bool muted)
    {
        if (!Available) throw new InvalidOperationException("Аудиосессия музыкального приложения пока недоступна.");
        foreach (var session in sessions.Values)
        {
            ComInterop.Check(session.Volume.SetMute(muted, writeContext));
            session.Muted = muted; session.LastRead = Stopwatch.GetTimestamp();
        }
        Muted = muted;
    }
    public void Reset()
    {
        var old = sessions.Values.ToArray(); sessions.Clear();
        foreach (var session in old) session.Dispose();
        Level = 0; Muted = false;
    }
    public AudioSnapshot Snapshot() => new(Available, Level, Muted, SubscriptionCount, RegistrationsCreated, NotificationCount, ExternalNotificationCount, OwnNotificationCount, AppliedNotificationCount, RecentNotifications);
    public void Dispose() { if (disposed) return; disposed = true; Reset(); }

}
