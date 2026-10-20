using System;
using System.Threading;

namespace YMusicTaskbar;

public sealed class SingleInstanceLease : IDisposable
{
    private const string MutexName = CompatibilityIds.MutexName;
    private const string QuitEvent = CompatibilityIds.QuitEvent;
    private readonly Mutex mutex;
    private readonly EventWaitHandle? exit;
    public bool Acquired { get; }
    public bool StopRequested => exit?.WaitOne(0) == true;
    public SingleInstanceLease(string mutexName = MutexName, string quitEventName = QuitEvent)
    {
        mutex = new Mutex(false, mutexName);
        try { Acquired = mutex.WaitOne(0); }
        catch (AbandonedMutexException) { Acquired = true; }
        if (Acquired) exit = new EventWaitHandle(false, EventResetMode.AutoReset, quitEventName);
    }
    public static void RequestExit(string quitEventName = QuitEvent)
    {
        try { using var signal = EventWaitHandle.OpenExisting(quitEventName); signal.Set(); }
        catch (WaitHandleCannotBeOpenedException) { }
    }
    public void Dispose() { exit?.Dispose(); if (Acquired) mutex.ReleaseMutex(); mutex.Dispose(); }
}
