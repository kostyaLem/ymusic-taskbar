namespace YMusicTaskbar;

public sealed record AudioSnapshot(bool Available, double Level, bool Muted, int SubscriptionCount,
    int RegistrationsCreated, long NotificationCount, long ExternalNotificationCount,
    long OwnNotificationCount, long AppliedNotificationCount, VolumeNotification[] RecentNotifications);
