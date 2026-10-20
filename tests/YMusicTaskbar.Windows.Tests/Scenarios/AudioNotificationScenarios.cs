using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using YMusicTaskbar;
using Forms = System.Windows.Forms;

internal static class AudioNotificationScenarios
{
    public static async Task RunAsync(IntegrationFixture test, JsonElement snapshot)
    {
        using var observer = new AudioService(); using var writer = new AudioService();
        observer.Refresh(); writer.Refresh();
        test.Check(observer.SubscriptionCount > 0, "Native volume callback registered");
        int registrations = observer.RegistrationsCreated;
        observer.Refresh();
        test.Check(observer.RegistrationsCreated == registrations, "Discovery preserves existing subscriptions");
        var notifications = Channel.CreateUnbounded<VolumeNotification>();
        observer.VolumeChanged += (_, change) => Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            observer.AcceptNotification(change); notifications.Writer.TryWrite(change);
        }));
        async Task<VolumeNotification> Wait(Func<VolumeNotification, bool> matches)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            while (true) { var value = await notifications.Reader.ReadAsync(timeout.Token); if (matches(value)) return value; }
        }
        double original = writer.Level; bool muted = writer.Muted;
        double target = original > 3 ? original - 2 : original + 2;
        try
        {
            writer.SetLevel(target);
            var notification = await Wait(n => !n.IsOwnChange && Math.Abs(n.Level - target) < 0.05);
            test.Check(Math.Abs(observer.Level - target) < 0.05, "External volume updates observer without polling");
            test.Check(observer.ExternalNotificationCount > 0 && observer.AppliedNotificationCount > 0, "External event context recognized and applied");
            var hwnd = new IntPtr(snapshot.GetProperty("Ui").GetProperty("Handle").GetInt64());
            var panel = AutomationElement.FromHandle(hwnd);
            var slider = panel.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "Volume"));
            var range = (RangeValuePattern)slider.GetCurrentPattern(RangeValuePattern.Pattern);
            var clock = Stopwatch.StartNew();
            while (Math.Abs(await Task.Run(() => range.Current.Value) - target) > 0.05 && clock.ElapsedMilliseconds < 800) await Task.Delay(30);
            test.Check(Math.Abs(await Task.Run(() => range.Current.Value) - target) < 0.05, "External event immediately updates WPF volume control");
            writer.SetMuted(!muted);
            await Wait(n => !n.IsOwnChange && n.Muted == !muted);
            test.Check(observer.Muted == !muted && Math.Abs(observer.Level - target) < 0.05, "External mute updates observer without polling");
            observer.SetLevel(target + 1);
            var ownChange = await Wait(n => n.IsOwnChange && Math.Abs(n.Level - target - 1) < 0.05);
            test.Check(observer.OwnNotificationCount > 0 && !observer.AcceptNotification(ownChange), "Own context does not feed back into UI state");
            test.Check(!observer.AcceptNotification(notification), "Queued old event cannot overwrite a newer local write");
            observer.Reset(); long count = observer.NotificationCount;
            writer.SetLevel(original); await Task.Delay(250);
            test.Check(observer.NotificationCount == count, "Unregistered observer stops receiving callbacks");
            observer.Refresh();
            test.Check(observer.RegistrationsCreated > registrations, "Observer reconnects after reset");
            test.Check(!observer.AcceptNotification(notification), "Old registration callback is rejected after reconnect");
            writer.SetLevel(target);
            await Wait(n => !n.IsOwnChange && Math.Abs(n.Level - target) < 0.05);
            test.Check(Math.Abs(observer.Level - target) < 0.05, "Reconnected subscription receives real changes");
        }
        finally { writer.SetLevel(original); writer.SetMuted(muted); }
    }
}
