using System;
using System.IO;

namespace YMusicTaskbar;

public sealed record LaunchOptions(bool Diagnose, bool SmokeTest, bool TestHost, bool Quit, bool? Compact, string Output)
{
    public bool Timed => SmokeTest || TestHost;
    public static LaunchOptions Parse(string[] arguments)
    {
        bool Has(string flag) => Array.IndexOf(arguments, flag) >= 0;
        int output = Array.IndexOf(arguments, "--output");
        if (output >= 0 && output + 1 >= arguments.Length) throw new ArgumentException("Укажите папку после --output.");
        string directory = output >= 0 ? arguments[output + 1] : Path.Combine(AppPaths.DirectoryPath, "diagnostics");
        return new(Has("--diagnose"), Has("--smoke-test"), Has("--test-host"), Has("--quit"),
            Has("--full") ? false : Has("--compact") ? true : null, Path.GetFullPath(directory));
    }
}
