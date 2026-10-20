using System.Runtime.InteropServices;

namespace YMusicTaskbar;

internal static class ComInterop
{
    public static void Check(int result) => Marshal.ThrowExceptionForHR(result);
    public static void Release(object? instance)
    {
        if (instance is not null && Marshal.IsComObject(instance)) Marshal.ReleaseComObject(instance);
    }
}
