using YMusicTaskbar;

namespace YMusicTaskbar.AppTests;

internal static class SingleInstanceScenarios
{
    public static void Run(Action<bool, string> check)
    {
        string mutexName = @"Local\YMusicTaskbar.Tests." + Guid.NewGuid().ToString("N");
        string quitName = mutexName + ".Quit";
        using var retainedHandle = new Mutex(false, mutexName);
        using (var first = new SingleInstanceLease(mutexName, quitName))
        {
            check(first.Acquired, "Unowned existing mutex permits startup");
            bool secondAcquired = true;
            var competitor = new Thread(() => { using var second = new SingleInstanceLease(mutexName, quitName); secondAcquired = second.Acquired; });
            competitor.Start(); competitor.Join();
            check(!secondAcquired, "Running instance prevents competing thread acquisition");
            SingleInstanceLease.RequestExit(quitName);
            check(first.StopRequested && !first.StopRequested, "Graceful exit signal is consumed once");
        }
        using (var next = new SingleInstanceLease(mutexName, quitName)) check(next.Acquired, "Released mutex can be acquired even with retained handle");
        var abandonedOwner = new Thread(() => retainedHandle.WaitOne());
        abandonedOwner.Start(); abandonedOwner.Join();
        using var recovered = new SingleInstanceLease(mutexName, quitName);
        check(recovered.Acquired, "Abandoned mutex is recovered after old process terminates");
    }
}
