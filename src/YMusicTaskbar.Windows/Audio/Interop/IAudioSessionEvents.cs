using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace YMusicTaskbar;

[ComVisible(true), Guid("24918ACC-64B3-37C1-8CA9-74A66E9957A8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioSessionEvents
{
    [PreserveSig] int OnDisplayNameChanged([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr context);
    [PreserveSig] int OnIconPathChanged([MarshalAs(UnmanagedType.LPWStr)] string path, IntPtr context);
    [PreserveSig] int OnSimpleVolumeChanged(float volume, [MarshalAs(UnmanagedType.Bool)] bool mute, IntPtr context);
    [PreserveSig] int OnChannelVolumeChanged(uint count, IntPtr volumes, uint channel, IntPtr context);
    [PreserveSig] int OnGroupingParamChanged(IntPtr grouping, IntPtr context);
    [PreserveSig] int OnStateChanged(int state);
    [PreserveSig] int OnSessionDisconnected(int reason);
}
